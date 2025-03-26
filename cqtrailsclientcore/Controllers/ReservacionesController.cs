using cqtrailsclientcore.Context;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;



[ApiController]
[Route("api/[controller]")]
public class ReservacionesController:ControllerBase
{


    private readonly MyDbContext _db;

    public ReservacionesController(MyDbContext db)
    {

        _db = db;

    }




    [HttpGet("MisReservaciones/{userid}")]
    public async Task<IActionResult> GetReservaciones(int userid)
    {
        

        var Reservaciones =
            await _db.Reservaciones.
                Include(o => o.IdUsuarioNavigation).
                Where(o => o.IdUsuario == userid).ToListAsync();

        return Ok(Reservaciones);
        
    }
    
    
    
    [HttpPost]
    public async Task<IActionResult> HacerReservacion([FromBody] int userId)
    {
        try
        {
            // Verificar si el usuario existe
            var usuario = await _db.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Obtener el carrito del usuario
            var carrito = await _db.Carrito
                .Where(c => c.usuario_id == userId)
                .FirstOrDefaultAsync();

            if (carrito == null)
            {
                return NotFound("El usuario no tiene un carrito");
            }

            // Obtener los items del carrito
            var detallesCarrito = await _db.DetalleCarrito
                .Include(d => d.Vehiculo)
                .Where(d => d.CarritoId == carrito.id)
                .ToListAsync();

            if (detallesCarrito == null || !detallesCarrito.Any())
            {
                return NotFound("No hay items en el carrito para crear una reservación");
            }

            // Convertir fechas UTC a fechas locales (sin información de zona horaria)
            var fechaActual = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var fechaInicio = DateTime.SpecifyKind(
                detallesCarrito.First().FechaInicio.ToDateTime(TimeOnly.MinValue), 
                DateTimeKind.Unspecified);
            var fechaFin = DateTime.SpecifyKind(
                detallesCarrito.First().FechaFin.ToDateTime(TimeOnly.MinValue), 
                DateTimeKind.Unspecified);

            // Crear la reservación
            var nuevaReservacion = new Reservacione
            {
                IdUsuario = userId,
                FechaReservacion = fechaActual,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Pendiente"
                // Puedes agregar más campos según sea necesario
            };

            _db.Reservaciones.Add(nuevaReservacion);
            await _db.SaveChangesAsync(); // Guardar para obtener el ID de la reservación

            // Asociar los vehículos a la reservación
            foreach (var detalle in detallesCarrito)
            {
                var vehiculoReservacion = new VehiculosReservacione
                {
                    IdReservacion = nuevaReservacion.IdReservacion,
                    IdVehiculo = detalle.VehiculoId,
                    EstadoAsignacion = "Activa"
                    // Puedes agregar más campos según sea necesario
                };

                _db.VehiculosReservaciones.Add(vehiculoReservacion);
            }

            // Vaciar el carrito después de crear la reservación
            _db.DetalleCarrito.RemoveRange(detallesCarrito);
            
            await _db.SaveChangesAsync();

            // Retornar la reservación creada con sus detalles
            var reservacionCreada = await _db.Reservaciones
                .Include(r => r.IdUsuarioNavigation)
                .Include(r => r.VehiculosReservaciones)
                    .ThenInclude(vr => vr.IdVehiculoNavigation)
                .FirstOrDefaultAsync(r => r.IdReservacion == nuevaReservacion.IdReservacion);

            return Ok(reservacionCreada);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
    
    
    
    
}