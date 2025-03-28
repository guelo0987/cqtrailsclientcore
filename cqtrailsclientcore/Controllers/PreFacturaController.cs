using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreFacturaController : ControllerBase
{
    private readonly MyDbContext _db;

    public PreFacturaController(MyDbContext db)
    {
        _db = db;
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
            return Ok(prefacturaDTO);
        }

        // Crear nueva prefactura si no existe
        var nuevaPrefactura = new PreFactura
        {
            IdReservacion = idreservacion,
            CostoVehiculo = reservacion.SubTotal,
            CostoTotal = reservacion.Total,
            ArchivoPdf = "prueba"
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