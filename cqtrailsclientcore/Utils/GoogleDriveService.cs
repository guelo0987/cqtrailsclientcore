using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Drive.v3.Data;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace cqtrailsclientcore.Utils;

public class GoogleDriveService
{
    private readonly string _credentialsPath;
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly string _folderId = "1hHcEJJ1EM3lU0wXs2Iaf0VJtvcAzS4gc"; // ID del folder de Google Drive

    public GoogleDriveService(string webRootPath, ILogger<GoogleDriveService> logger)
    {
        // Intentar encontrar el archivo de credenciales en varias ubicaciones
        string[] possiblePaths = new string[]
        {
            Path.Combine(webRootPath, "credentials.json"),
            Path.Combine(webRootPath, "wwwroot", "credentials.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "credentials.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "credentials.json")
        };
        
        foreach (var path in possiblePaths)
        {
            if (System.IO.File.Exists(path))
            {
                _credentialsPath = path;
                logger.LogInformation($"Archivo de credenciales encontrado en: {path}");
                break;
            }
        }
        
        if (string.IsNullOrEmpty(_credentialsPath))
        {
            // Si no encontramos el archivo, usar la ubicación default
            _credentialsPath = Path.Combine(webRootPath, "credentials.json");
            logger.LogWarning($"No se encontró el archivo de credenciales en ninguna ubicación. Se usará: {_credentialsPath}");
        }
        
        _logger = logger;
    }

    public async Task<string> UploadFile(string localFilePath, string fileName, string mimeType = "application/pdf")
    {
        try
        {
            // Verificar que el archivo existe
            if (!System.IO.File.Exists(localFilePath))
            {
                _logger.LogError($"El archivo no existe: {localFilePath}");
                throw new FileNotFoundException($"El archivo no existe: {localFilePath}");
            }
            
            // Verificar que el archivo de credenciales existe
            if (!System.IO.File.Exists(_credentialsPath))
            {
                _logger.LogError($"Archivo de credenciales no encontrado: {_credentialsPath}");
                throw new FileNotFoundException($"Archivo de credenciales no encontrado: {_credentialsPath}");
            }

            // Cargar credenciales de Google
            var credential = GoogleCredential.FromFile(_credentialsPath)
                .CreateScoped(DriveService.ScopeConstants.Drive);

            // Crear servicio de Google Drive
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "CQTrails PDF Uploader"
            });

            // Crear archivo de metadatos sin especificar el folder
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName
            };
            
            // Intentar usar el folder específico solo si está configurado
            try
            {
                if (!string.IsNullOrEmpty(_folderId))
                {
                    var folderRequest = service.Files.Get(_folderId);
                    folderRequest.Fields = "id,name";
                    var folder = await folderRequest.ExecuteAsync();
                    if (folder != null && !string.IsNullOrEmpty(folder.Id))
                    {
                        fileMetadata.Parents = new[] { _folderId };
                        _logger.LogInformation($"Usando folder: {folder.Name} (ID: {folder.Id})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Folder no encontrado o no accesible: {ex.Message}. Se subirá a la raíz de Drive.");
            }

            // Método alternativo usando CreateMediaUpload
            using (var stream = new FileStream(localFilePath, FileMode.Open))
            {
                var request = service.Files.Create(fileMetadata, stream, mimeType);
                request.Fields = "id,webViewLink,webContentLink";
                
                _logger.LogInformation($"Iniciando subida del archivo: {fileName}");
                var progress = await request.UploadAsync();
                
                if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
                {
                    _logger.LogError($"Error al subir archivo: {progress.Exception?.Message}");
                    throw new Exception($"Error al subir archivo a Google Drive: {progress.Exception?.Message}");
                }
                
                var file = request.ResponseBody;
                _logger.LogInformation($"Archivo subido correctamente con ID: {file.Id}");
                
                // Configurar permisos para que cualquiera pueda ver el archivo
                try
                {
                    var permission = new Permission
                    {
                        Type = "anyone",
                        Role = "reader"
                    };
                    
                    await service.Permissions.Create(permission, file.Id).ExecuteAsync();
                    _logger.LogInformation("Permisos públicos configurados correctamente");
                    
                    // Obtener el enlace actualizado después de cambiar permisos
                    var getRequest = service.Files.Get(file.Id);
                    getRequest.Fields = "webViewLink,webContentLink";
                    var updatedFile = await getRequest.ExecuteAsync();
                    
                    string fileUrl = updatedFile.WebViewLink ?? updatedFile.WebContentLink;
                    _logger.LogInformation($"URL del archivo: {fileUrl}");
                    
                    return fileUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"No se pudieron configurar permisos públicos: {ex.Message}");
                    // Si falla al configurar permisos, devolver el enlace original
                    return file.WebViewLink ?? file.WebContentLink ?? 
                           $"https://drive.google.com/file/d/{file.Id}/view?usp=sharing";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al subir archivo a Google Drive: {ex.Message}");
            throw new Exception($"Error al subir archivo a Google Drive: {ex.Message}", ex);
        }
    }
} 