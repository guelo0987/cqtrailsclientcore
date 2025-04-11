using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using cqtrailsclientcore.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreFacturaController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PreFacturaController(MyDbContext db, IWebHostEnvironment webHostEnvironment)
    {
        _db = db;
        _webHostEnvironment = webHostEnvironment;
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

        // Verificar si ya existe una prefactura
        var prefacturaExistente = await _db.PreFacturas
            .Include(p => p.IdReservacionNavigation)
                .ThenInclude(r => r.IdUsuarioNavigation)
            .Include(p => p.IdReservacionNavigation.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);

        if (prefacturaExistente != null)
        {
            // Mapear a DTO
            var prefacturaDTO = MapToPreFacturaDTO(prefacturaExistente);
            
            // Verificar si ya se generó el PDF
            if (string.IsNullOrEmpty(prefacturaExistente.ArchivoPdf) || 
                prefacturaExistente.ArchivoPdf == "prueba" || 
                prefacturaExistente.ArchivoPdf == "pendiente")
            {
                // Generar el PDF si no existe
                string rutaPdf = PdfGenerator.GenerarPrefacturaPDF(prefacturaDTO, _webHostEnvironment.WebRootPath);
                
                // Actualizar la ruta del PDF en la base de datos
                prefacturaExistente.ArchivoPdf = rutaPdf;
                _db.PreFacturas.Update(prefacturaExistente);
                await _db.SaveChangesAsync();
                
                // Actualizar el DTO
                prefacturaDTO.ArchivoPdf = rutaPdf;
            }
            
            return Ok(prefacturaDTO);
        }

        // Crear nueva prefactura si no existe
        var nuevaPrefactura = new PreFactura
        {
            IdReservacion = idreservacion,
            CostoVehiculo = reservacion.SubTotal,
            CostoTotal = reservacion.Total,
            FechaGeneracion = DateTime.Now,
            ArchivoPdf = "pendiente" // Marcar como pendiente para generar PDF
        };

        try
        {
            _db.PreFacturas.Add(nuevaPrefactura);
            await _db.SaveChangesAsync();
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
                var prefacturaDTO = MapToPreFacturaDTO(prefacturaCreada);
                
                // Generar PDF si no existe
                if (string.IsNullOrEmpty(prefacturaCreada.ArchivoPdf) || 
                    prefacturaCreada.ArchivoPdf == "pendiente" || 
                    prefacturaCreada.ArchivoPdf == "prueba")
                {
                    string rutaPdf = PdfGenerator.GenerarPrefacturaPDF(prefacturaDTO, _webHostEnvironment.WebRootPath);
                    
                    // Actualizar la ruta del PDF
                    prefacturaCreada.ArchivoPdf = rutaPdf;
                    _db.PreFacturas.Update(prefacturaCreada);
                    await _db.SaveChangesAsync();
                    
                    // Actualizar el DTO
                    prefacturaDTO.ArchivoPdf = rutaPdf;
                }
                
                return Ok(prefacturaDTO);
            }
            
            return StatusCode(500, "Error al generar la prefactura");
        }

        // Recargar la entidad con las relaciones
        var prefacturaActualizada = await _db.PreFacturas
            .Include(p => p.IdReservacionNavigation)
                .ThenInclude(r => r.IdUsuarioNavigation)
            .Include(p => p.IdReservacionNavigation.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);

        if (prefacturaActualizada == null)
        {
            return StatusCode(500, "Error al recuperar la prefactura creada");
        }

        var prefacturaActualizadaDTO = MapToPreFacturaDTO(prefacturaActualizada);
        
        // Generar el PDF para la nueva prefactura
        string rutaPdfNueva = PdfGenerator.GenerarPrefacturaPDF(prefacturaActualizadaDTO, _webHostEnvironment.WebRootPath);
        
        // Actualizar la ruta del PDF en la base de datos
        prefacturaActualizada.ArchivoPdf = rutaPdfNueva;
        _db.PreFacturas.Update(prefacturaActualizada);
        await _db.SaveChangesAsync();
        
        // Actualizar el DTO con la ruta del PDF
        prefacturaActualizadaDTO.ArchivoPdf = rutaPdfNueva;
        
        return Ok(prefacturaActualizadaDTO);
    }

    private PreFacturaDTO MapToPreFacturaDTO(PreFactura prefactura)
    {
        var reservacion = prefactura.IdReservacionNavigation;
        
        return new PreFacturaDTO
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
                    EstadoAsignacion = vr.EstadoAsignacion
                }).ToList()
            } : null
        };
    }
}