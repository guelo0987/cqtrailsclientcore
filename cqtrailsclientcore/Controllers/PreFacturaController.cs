using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using cqtrailsclientcore.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreFacturaController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly GoogleDriveService _googleDriveService;
    private readonly ILogger<PreFacturaController> _logger;

    public PreFacturaController(
        MyDbContext db, 
        IWebHostEnvironment webHostEnvironment,
        GoogleDriveService googleDriveService,
        ILogger<PreFacturaController> logger)
    {
        _db = db;
        _webHostEnvironment = webHostEnvironment;
        _googleDriveService = googleDriveService;
        _logger = logger;
    }

    [HttpGet("Prefactura/{idreservacion}/{userid}")]
    public async Task<IActionResult> GetPrefactura(int idreservacion, int userid)
    {
        if (idreservacion <= 0)
            return BadRequest("ID de reservación inválido");

        var reservacion = await _db.Reservaciones
            .Include(o => o.IdUsuarioNavigation)
            .Include(o => o.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .FirstOrDefaultAsync(o => o.IdReservacion == idreservacion && o.IdUsuario == userid);

        if (reservacion == null)
            return BadRequest("Reservación no encontrada");

        if (reservacion.Estado != "Aceptada")
            return BadRequest("La reservación no está aprobada");
            
        // Log vehicle information
        _logger.LogInformation($"Reservación {idreservacion} con {reservacion.VehiculosReservaciones.Count} vehículos encontrados");
        foreach (var vr in reservacion.VehiculosReservaciones)
        {
            _logger.LogInformation($"Vehículo en reservación: ID:{vr.IdVehiculo}, Placa:{vr.IdVehiculoNavigation?.Placa}");
        }

        // Verificar si ya existe una prefactura
        var prefacturaExistente = await _db.PreFacturas
            .Include(p => p.IdReservacionNavigation)
                .ThenInclude(r => r.IdUsuarioNavigation)
            .Include(p => p.IdReservacionNavigation.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);

        if (prefacturaExistente != null)
        {
            // Log vehicle information in existing prefactura
            _logger.LogInformation($"Prefactura existente {prefacturaExistente.IdPreFactura} con {prefacturaExistente.IdReservacionNavigation.VehiculosReservaciones.Count} vehículos");
            
            // Mapear a DTO
            var prefacturaDTO = MapToPreFacturaDTO(prefacturaExistente);
            
            // Log vehicle info in DTO
            if (prefacturaDTO.Reservacion?.Vehiculos != null)
            {
                _logger.LogInformation($"Vehículos en DTO: {prefacturaDTO.Reservacion.Vehiculos.Count}");
            }
            else
            {
                _logger.LogWarning("No hay vehículos en el DTO de la prefactura");
            }
            
            // Verificar si ya se generó el PDF o si es necesario actualizar a Google Drive
            bool needsGoogleDriveUrl = string.IsNullOrEmpty(prefacturaExistente.ArchivoPdf) || 
                prefacturaExistente.ArchivoPdf == "prueba" || 
                prefacturaExistente.ArchivoPdf == "pendiente" ||
                !prefacturaExistente.ArchivoPdf.StartsWith("https://");
                
            if (needsGoogleDriveUrl)
            {
                // Generar el PDF y subirlo a Google Drive
                try
                {
                    string googleDriveUrl = await PdfGenerator.GenerarPrefacturaPDFAsync(
                        prefacturaDTO, 
                        _webHostEnvironment.WebRootPath, 
                        _googleDriveService,
                        _logger);
                    
                    // Actualizar la ruta del PDF con la URL de Google Drive
                    prefacturaExistente.ArchivoPdf = googleDriveUrl;
                    _db.PreFacturas.Update(prefacturaExistente);
                    await _db.SaveChangesAsync();
                    
                    // Actualizar el DTO
                    prefacturaDTO.ArchivoPdf = googleDriveUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al generar PDF: {ex.Message}");
                    return StatusCode(500, $"Error al generar PDF: {ex.Message}");
                }
            }
            
            return Ok(prefacturaDTO);
        }

        // Crear una nueva prefactura
        
        // Calcular coste
        var costoVehiculo = CalcularCostoVehiculo(reservacion);
        decimal? costoAdicional = null;
        if (!string.IsNullOrEmpty(reservacion.RequerimientosAdicionales))
        {
            costoAdicional = 50;
        }

        var total = costoVehiculo + (costoAdicional ?? 0);

        var nuevaPrefactura = new PreFactura
        {
            IdReservacion = idreservacion,
            CostoVehiculo = costoVehiculo,
            CostoAdicional = costoAdicional,
            CostoTotal = total,
            FechaGeneracion = DateTime.Now,
            ArchivoPdf = "pendiente" // Pendiente de generar
        };

        try
        {
            _db.PreFacturas.Add(nuevaPrefactura);
            await _db.SaveChangesAsync();
            
            // Recargar la prefactura con todos los datos relacionados
            _db.Entry(nuevaPrefactura).State = EntityState.Detached;
            
            var prefacturaCompleta = await _db.PreFacturas
                .Include(p => p.IdReservacionNavigation)
                    .ThenInclude(r => r.IdUsuarioNavigation)
                .Include(p => p.IdReservacionNavigation.VehiculosReservaciones)
                    .ThenInclude(vr => vr.IdVehiculoNavigation)
                .FirstOrDefaultAsync(p => p.IdPreFactura == nuevaPrefactura.IdPreFactura);
            
            if (prefacturaCompleta == null)
            {
                return StatusCode(500, "Error al recargar la prefactura");
            }
            
            // Log vehicle information in new prefactura
            _logger.LogInformation($"Nueva prefactura {prefacturaCompleta.IdPreFactura} con {prefacturaCompleta.IdReservacionNavigation.VehiculosReservaciones.Count} vehículos");
            
            var prefacturaDTO = MapToPreFacturaDTO(prefacturaCompleta);
            
            // Log vehicle info in DTO
            if (prefacturaDTO.Reservacion?.Vehiculos != null)
            {
                _logger.LogInformation($"Vehículos en DTO de nueva prefactura: {prefacturaDTO.Reservacion.Vehiculos.Count}");
                foreach (var v in prefacturaDTO.Reservacion.Vehiculos)
                {
                    _logger.LogInformation($"Vehículo mapeado: ID:{v.IdVehiculo}, Placa:{v.Placa}, Modelo:{v.Modelo}");
                }
            }
            
            // Generar PDF y subir a Google Drive
            try
            {
                string googleDriveUrl = await PdfGenerator.GenerarPrefacturaPDFAsync(
                    prefacturaDTO, 
                    _webHostEnvironment.WebRootPath, 
                    _googleDriveService,
                    _logger);
                
                // Actualizar la ruta del PDF con la URL de Google Drive
                prefacturaCompleta.ArchivoPdf = googleDriveUrl;
                _db.PreFacturas.Update(prefacturaCompleta);
                await _db.SaveChangesAsync();
                
                // Actualizar el DTO
                prefacturaDTO.ArchivoPdf = googleDriveUrl;
                
                return Ok(prefacturaDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al generar PDF: {ex.Message}");
                return StatusCode(500, $"Error al generar PDF: {ex.Message}");
            }
        }
        catch (DbUpdateException ex)
        {
            // Manejar posible inserción concurrente
            _db.Entry(nuevaPrefactura).State = EntityState.Detached;
            
            var prefacturaCreada = await _db.PreFacturas
                .Include(p => p.IdReservacionNavigation)
                    .ThenInclude(r => r.IdUsuarioNavigation)
                .Include(p => p.IdReservacionNavigation.VehiculosReservaciones)
                    .ThenInclude(vr => vr.IdVehiculoNavigation)
                .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);
            
            if (prefacturaCreada != null)
            {
                // Log vehicle information in concurrent prefactura
                _logger.LogInformation($"Prefactura concurrente {prefacturaCreada.IdPreFactura} con {prefacturaCreada.IdReservacionNavigation.VehiculosReservaciones.Count} vehículos");
                
                var prefacturaDTO = MapToPreFacturaDTO(prefacturaCreada);
                
                // Log vehicle info in DTO
                if (prefacturaDTO.Reservacion?.Vehiculos != null)
                {
                    _logger.LogInformation($"Vehículos en DTO de prefactura concurrente: {prefacturaDTO.Reservacion.Vehiculos.Count}");
                }
                
                // Generar PDF y subir a Google Drive
                try 
                {
                    string googleDriveUrl = await PdfGenerator.GenerarPrefacturaPDFAsync(
                        prefacturaDTO, 
                        _webHostEnvironment.WebRootPath, 
                        _googleDriveService,
                        _logger);
                    
                    // Actualizar la ruta del PDF con la URL de Google Drive
                    prefacturaCreada.ArchivoPdf = googleDriveUrl;
                    _db.PreFacturas.Update(prefacturaCreada);
                    await _db.SaveChangesAsync();
                    
                    // Actualizar el DTO
                    prefacturaDTO.ArchivoPdf = googleDriveUrl;
                }
                catch (Exception ex2)
                {
                    _logger.LogError($"Error al generar PDF: {ex2.Message}");
                    return StatusCode(500, $"Error al generar PDF: {ex2.Message}");
                }
                
                return Ok(prefacturaDTO);
            }
            
            return StatusCode(500, $"Error al crear prefactura: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al crear prefactura: {ex.Message}");
        }
    }

    // Método para calcular el costo de los vehículos en una reservación
    private decimal CalcularCostoVehiculo(Reservacione reservacion)
    {
        decimal costo = 0;
        int diasReservacion = (int)(reservacion.FechaFin - reservacion.FechaInicio).TotalDays;
        if (diasReservacion < 1) diasReservacion = 1;
        
        // Sumar el precio de cada vehículo
        foreach (var vehiculoReservacion in reservacion.VehiculosReservaciones)
        {
            var vehiculo = vehiculoReservacion.IdVehiculoNavigation;
            if (vehiculo != null)
            {
                costo += vehiculo.Price * diasReservacion;
            }
        }
        
        return costo;
    }

    // Método privado para mapear de entidad a DTO
    private PreFacturaDTO MapToPreFacturaDTO(PreFactura prefactura)
    {
        var reservacion = prefactura.IdReservacionNavigation;
        
        // Log detailed mapping info
        _logger.LogInformation($"Mapeando prefactura {prefactura.IdPreFactura} para reservación {prefactura.IdReservacion}");
        
        if (reservacion?.VehiculosReservaciones != null)
        {
            _logger.LogInformation($"Mapeando {reservacion.VehiculosReservaciones.Count} vehículos");
            foreach (var vr in reservacion.VehiculosReservaciones)
            {
                _logger.LogInformation($"Mapeando vehículo: ID:{vr.IdVehiculo}, Placa:{vr.IdVehiculoNavigation?.Placa}");
            }
        }
        else
        {
            _logger.LogWarning("No hay vehículos para mapear o la reservación es nula");
        }
        
        var result = new PreFacturaDTO
        {
            IdPreFactura = prefactura.IdPreFactura,
            IdReservacion = prefactura.IdReservacion,
            CostoVehiculo = prefactura.CostoVehiculo,
            CostoAdicional = prefactura.CostoAdicional,
            CostoTotal = prefactura.CostoTotal,
            FechaGeneracion = prefactura.FechaGeneracion,
            ArchivoPdf = prefactura.ArchivoPdf,
            Reservacion = reservacion != null ? new ReservacioneDTO
            {
                IdReservacion = reservacion.IdReservacion,
                IdUsuario = reservacion.IdUsuario,
                FechaInicio = reservacion.FechaInicio,
                FechaFin = reservacion.FechaFin,
                RutaPersonalizada = reservacion.RutaPersonalizada,
                RequerimientosAdicionales = reservacion.RequerimientosAdicionales,
                Estado = reservacion.Estado,
                FechaReservacion = reservacion.FechaReservacion,
                FechaConfirmacion = reservacion.FechaConfirmacion,
                Total = reservacion.Total,
                SubTotal = reservacion.SubTotal,
                CiudadInicioId = reservacion.ciudadinicioid,
                CiudadFinId = reservacion.ciudadfinid,
                Usuario = reservacion.IdUsuarioNavigation != null ? new UsuarioBasicoDTO
                {
                    IdUsuario = reservacion.IdUsuarioNavigation.IdUsuario,
                    Nombre = reservacion.IdUsuarioNavigation.Nombre,
                    Apellido = reservacion.IdUsuarioNavigation.Apellido,
                    Email = reservacion.IdUsuarioNavigation.Email
                } : null,
                Vehiculos = reservacion.VehiculosReservaciones.Select(vr => new VehiculoReservacionDTO
                {
                    IdVehiculo = vr.IdVehiculo,
                    Placa = vr.IdVehiculoNavigation.Placa,
                    Modelo = vr.IdVehiculoNavigation.Modelo,
                    TipoVehiculo = vr.IdVehiculoNavigation.TipoVehiculo,
                    Capacidad = vr.IdVehiculoNavigation.Capacidad,
                    Ano = vr.IdVehiculoNavigation.Ano,
                    EstadoAsignacion = vr.EstadoAsignacion,
                    ImageUrl = vr.IdVehiculoNavigation.Image_url
                }).ToList()
            } : null
        };
        
        // Log result
        if (result.Reservacion?.Vehiculos != null)
        {
            _logger.LogInformation($"DTO generado con {result.Reservacion.Vehiculos.Count} vehículos");
        }
        else
        {
            _logger.LogWarning("DTO generado sin vehículos");
        }
        
        return result;
    }
}