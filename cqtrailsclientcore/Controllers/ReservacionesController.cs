using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservacionesController : ControllerBase
{
    private readonly MyDbContext _db;

    public ReservacionesController(MyDbContext db)
    {
        _db = db;
    }

    [HttpGet("MisReservaciones/{userid}")]
    public async Task<IActionResult> GetReservaciones(int userid)
    {
        var reservaciones = await _db.Reservaciones
            .Include(o => o.IdUsuarioNavigation)
            .Include(o => o.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .Where(o => o.IdUsuario == userid)
            .ToListAsync();

        // Mapear a DTO para evitar exponer datos sensibles
        var reservacionesDTO = reservaciones.Select(r => new ReservacioneDTO
        {
            IdReservacion = r.IdReservacion,
            IdUsuario = r.IdUsuario,
            FechaInicio = r.FechaInicio,
            FechaFin = r.FechaFin,
            RutaPersonalizada = r.RutaPersonalizada,
            RequerimientosAdicionales = r.RequerimientosAdicionales,
            Estado = r.Estado,
            FechaReservacion = r.FechaReservacion,
            FechaConfirmacion = r.FechaConfirmacion,
            Total = r.Total,
            SubTotal = r.SubTotal,
            Usuario = r.IdUsuarioNavigation != null ? new UsuarioBasicoDTO
            {
                IdUsuario = r.IdUsuarioNavigation.IdUsuario,
                Nombre = r.IdUsuarioNavigation.Nombre,
                Apellido = r.IdUsuarioNavigation.Apellido,
                Email = r.IdUsuarioNavigation.Email
            } : null,
            Vehiculos = r.VehiculosReservaciones.Select(vr => new VehiculoReservacionDTO
            {
                IdVehiculo = vr.IdVehiculo,
                Placa = vr.IdVehiculoNavigation.Placa,
                Modelo = vr.IdVehiculoNavigation.Modelo,
                TipoVehiculo = vr.IdVehiculoNavigation.TipoVehiculo,
                Capacidad = vr.IdVehiculoNavigation.Capacidad,
                Ano = vr.IdVehiculoNavigation.Ano,
                EstadoAsignacion = vr.EstadoAsignacion
            }).ToList()
        }).ToList();

        return Ok(reservacionesDTO);
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
            
            var subTotal = detallesCarrito.Sum(d => d.SubTotal);
            var total = detallesCarrito.Sum(d => d.Total);

            // Crear la reservación
            var nuevaReservacion = new Reservacione
            {
                IdUsuario = userId,
                FechaReservacion = fechaActual,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = "Pendiente",
                Total = total,
                SubTotal = subTotal
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
                };

                _db.VehiculosReservaciones.Add(vehiculoReservacion);
            }

            // Vaciar el carrito después de crear la reservación
            _db.DetalleCarrito.RemoveRange(detallesCarrito);
            
            await _db.SaveChangesAsync();

            // Retornar la reservación creada con sus detalles (usando DTO)
            var reservacionCreada = await _db.Reservaciones
                .Include(r => r.IdUsuarioNavigation)
                .Include(r => r.VehiculosReservaciones)
                    .ThenInclude(vr => vr.IdVehiculoNavigation)
                .FirstOrDefaultAsync(r => r.IdReservacion == nuevaReservacion.IdReservacion);

            if (reservacionCreada == null)
            {
                return StatusCode(500, "Error al recuperar la reservación creada");
            }

            var reservacionDTO = new ReservacioneDTO
            {
                IdReservacion = reservacionCreada.IdReservacion,
                IdUsuario = reservacionCreada.IdUsuario,
                FechaInicio = reservacionCreada.FechaInicio,
                FechaFin = reservacionCreada.FechaFin,
                RutaPersonalizada = reservacionCreada.RutaPersonalizada,
                RequerimientosAdicionales = reservacionCreada.RequerimientosAdicionales,
                Estado = reservacionCreada.Estado,
                FechaReservacion = reservacionCreada.FechaReservacion,
                FechaConfirmacion = reservacionCreada.FechaConfirmacion,
                Total = reservacionCreada.Total,
                SubTotal = reservacionCreada.SubTotal,
                Usuario = reservacionCreada.IdUsuarioNavigation != null ? new UsuarioBasicoDTO
                {
                    IdUsuario = reservacionCreada.IdUsuarioNavigation.IdUsuario,
                    Nombre = reservacionCreada.IdUsuarioNavigation.Nombre,
                    Apellido = reservacionCreada.IdUsuarioNavigation.Apellido,
                    Email = reservacionCreada.IdUsuarioNavigation.Email
                } : null,
                Vehiculos = reservacionCreada.VehiculosReservaciones.Select(vr => new VehiculoReservacionDTO
                {
                    IdVehiculo = vr.IdVehiculo,
                    Placa = vr.IdVehiculoNavigation.Placa,
                    Modelo = vr.IdVehiculoNavigation.Modelo,
                    TipoVehiculo = vr.IdVehiculoNavigation.TipoVehiculo,
                    Capacidad = vr.IdVehiculoNavigation.Capacidad,
                    Ano = vr.IdVehiculoNavigation.Ano,
                    EstadoAsignacion = vr.EstadoAsignacion
                }).ToList()
            };

            return Ok(reservacionDTO);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}