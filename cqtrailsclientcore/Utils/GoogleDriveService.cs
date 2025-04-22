using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Drive.v3.Data;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;

namespace cqtrailsclientcore.Utils;

public class GoogleDriveService
{
    private readonly string _credentialsPath;
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly string _folderId = "1hHcEJJ1EM3lU0wXs2Iaf0VJtvcAzS4gc"; // ID del folder de Google Drive
    private GoogleCredential _credential;

    // Constantes para las variables de entorno
    private const string ENV_GOOGLE_CREDENTIALS_JSON = "GOOGLE_CREDENTIALS_JSON";
    private const string ENV_GOOGLE_TYPE = "GOOGLE_TYPE";
    private const string ENV_GOOGLE_PROJECT_ID = "GOOGLE_PROJECT_ID";
    private const string ENV_GOOGLE_PRIVATE_KEY_ID = "GOOGLE_PRIVATE_KEY_ID";
    private const string ENV_GOOGLE_PRIVATE_KEY = "GOOGLE_PRIVATE_KEY";
    private const string ENV_GOOGLE_CLIENT_EMAIL = "GOOGLE_CLIENT_EMAIL";
    private const string ENV_GOOGLE_CLIENT_ID = "GOOGLE_CLIENT_ID";
    private const string ENV_GOOGLE_AUTH_URI = "GOOGLE_AUTH_URI";
    private const string ENV_GOOGLE_TOKEN_URI = "GOOGLE_TOKEN_URI";
    private const string ENV_GOOGLE_AUTH_PROVIDER_X509_CERT_URL = "GOOGLE_AUTH_PROVIDER_X509_CERT_URL";
    private const string ENV_GOOGLE_CLIENT_X509_CERT_URL = "GOOGLE_CLIENT_X509_CERT_URL";
    private const string ENV_GOOGLE_UNIVERSE_DOMAIN = "GOOGLE_UNIVERSE_DOMAIN";

    public GoogleDriveService(string webRootPath, ILogger<GoogleDriveService> logger)
    {
        _logger = logger;

        // Intentar cargar credenciales desde variables de entorno primero
        if (TryLoadCredentialsFromEnvironment())
        {
            _logger.LogInformation("Credenciales de Google Drive cargadas exitosamente desde variables de entorno");
            return;
        }

        // Si no hay variables de entorno, buscar archivo de credenciales
        _logger.LogInformation("Buscando archivo de credenciales...");
        
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
                _logger.LogInformation($"Archivo de credenciales encontrado en: {path}");
                
                try
                {
                    _credential = GoogleCredential.FromFile(_credentialsPath)
                        .CreateScoped(DriveService.ScopeConstants.Drive);
                    _logger.LogInformation("Credenciales cargadas exitosamente desde archivo");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al cargar credenciales desde archivo: {ex.Message}");
                }
                
                break;
            }
        }
        
        if (string.IsNullOrEmpty(_credentialsPath))
        {
            // Si no encontramos el archivo, usar la ubicación default
            _credentialsPath = Path.Combine(webRootPath, "credentials.json");
            _logger.LogWarning($"No se encontró el archivo de credenciales en ninguna ubicación. Se usará: {_credentialsPath}");
        }
    }

    private bool TryLoadCredentialsFromEnvironment()
    {
        try
        {
            // Verificar si existe la variable con el JSON completo
            string credentialsJson = Environment.GetEnvironmentVariable(ENV_GOOGLE_CREDENTIALS_JSON);
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                // Crear un archivo temporal con las credenciales
                string tempPath = Path.GetTempFileName();
                try
                {
                    System.IO.File.WriteAllText(tempPath, credentialsJson);
                    _credential = GoogleCredential.FromFile(tempPath)
                        .CreateScoped(DriveService.ScopeConstants.Drive);
                    return true;
                }
                finally
                {
                    // Eliminar el archivo temporal
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            
            // Verificar si existen las variables individuales
            List<string> requiredVars = new List<string>
            {
                ENV_GOOGLE_TYPE, ENV_GOOGLE_PROJECT_ID, ENV_GOOGLE_PRIVATE_KEY_ID,
                ENV_GOOGLE_PRIVATE_KEY, ENV_GOOGLE_CLIENT_EMAIL, ENV_GOOGLE_CLIENT_ID,
                ENV_GOOGLE_AUTH_URI, ENV_GOOGLE_TOKEN_URI, ENV_GOOGLE_AUTH_PROVIDER_X509_CERT_URL,
                ENV_GOOGLE_CLIENT_X509_CERT_URL
            };
            
            // Verificar que todas las variables requeridas existan
            foreach (var varName in requiredVars)
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(varName)))
                {
                    _logger.LogDebug($"No se encontró la variable de entorno {varName}");
                    return false;
                }
            }
            
            // Construir el diccionario de credenciales
            var credentialsDict = new Dictionary<string, string>
            {
                { "type", Environment.GetEnvironmentVariable(ENV_GOOGLE_TYPE) },
                { "project_id", Environment.GetEnvironmentVariable(ENV_GOOGLE_PROJECT_ID) },
                { "private_key_id", Environment.GetEnvironmentVariable(ENV_GOOGLE_PRIVATE_KEY_ID) },
                { "private_key", Environment.GetEnvironmentVariable(ENV_GOOGLE_PRIVATE_KEY).Replace("\\n", "\n") },
                { "client_email", Environment.GetEnvironmentVariable(ENV_GOOGLE_CLIENT_EMAIL) },
                { "client_id", Environment.GetEnvironmentVariable(ENV_GOOGLE_CLIENT_ID) },
                { "auth_uri", Environment.GetEnvironmentVariable(ENV_GOOGLE_AUTH_URI) },
                { "token_uri", Environment.GetEnvironmentVariable(ENV_GOOGLE_TOKEN_URI) },
                { "auth_provider_x509_cert_url", Environment.GetEnvironmentVariable(ENV_GOOGLE_AUTH_PROVIDER_X509_CERT_URL) },
                { "client_x509_cert_url", Environment.GetEnvironmentVariable(ENV_GOOGLE_CLIENT_X509_CERT_URL) }
            };
            
            // Añadir universe_domain si está disponible
            string universeDomain = Environment.GetEnvironmentVariable(ENV_GOOGLE_UNIVERSE_DOMAIN);
            if (!string.IsNullOrEmpty(universeDomain))
            {
                credentialsDict.Add("universe_domain", universeDomain);
            }
            
            // Crear un archivo temporal con las credenciales
            string jsonCredentials = JsonSerializer.Serialize(credentialsDict);
            string tempFilePath = Path.GetTempFileName();
            
            try
            {
                System.IO.File.WriteAllText(tempFilePath, jsonCredentials);
                _credential = GoogleCredential.FromFile(tempFilePath)
                    .CreateScoped(DriveService.ScopeConstants.Drive);
                return true;
            }
            finally
            {
                // Eliminar el archivo temporal
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al cargar credenciales desde variables de entorno: {ex.Message}");
            return false;
        }
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
            
            // Verificar que tenemos credenciales válidas
            if (_credential == null)
            {
                // Si no tenemos credenciales en memoria, intentar cargarlas del archivo
                if (string.IsNullOrEmpty(_credentialsPath) || !System.IO.File.Exists(_credentialsPath))
                {
                    _logger.LogError("No se encontraron credenciales válidas para Google Drive");
                    throw new FileNotFoundException("No se encontraron credenciales válidas para Google Drive");
                }
                
                _credential = GoogleCredential.FromFile(_credentialsPath)
                    .CreateScoped(DriveService.ScopeConstants.Drive);
            }

            // Crear servicio de Google Drive
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
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