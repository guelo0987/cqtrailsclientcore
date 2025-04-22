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

    // Helper method to normalize DateTime values for PostgreSQL
    private DateTime NormalizeDateTime(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 
                           dateTime.Hour, dateTime.Minute, dateTime.Second, 
                           dateTime.Millisecond, DateTimeKind.Unspecified);
    }

    [HttpGet("MisReservaciones/{userid}")]
    public async Task<IActionResult> GetReservaciones(int userid)
    {
        var reservaciones = await _db.Reservaciones
            .Include(o => o.IdUsuarioNavigation)
            .Include(o => o.VehiculosReservaciones)
                .ThenInclude(vr => vr.IdVehiculoNavigation)
            .Include(o => o.CiudadInicioNavigation)
            .Include(o => o.CiudadFinNavigation)
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
            CiudadInicioId = r.ciudadinicioid,
            CiudadFinId = r.ciudadfinid,
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
                EstadoAsignacion = vr.EstadoAsignacion,
                ImageUrl = vr.IdVehiculoNavigation.Image_url
            }).ToList()
        }).ToList();

        return Ok(reservacionesDTO);
    }
    
    [HttpGet("Detalle/{userId}/{id}")]
public async Task<IActionResult> GetReservacionDetalle(int userId, int id)
{
    // Buscar la reservación que cumpla con el id de la reservación y que pertenezca al usuario indicado
    var reservacion = await _db.Reservaciones
        .Include(r => r.IdUsuarioNavigation)
        .Include(r => r.VehiculosReservaciones)
            .ThenInclude(vr => vr.IdVehiculoNavigation)
        .Include(r => r.CiudadInicioNavigation)
        .Include(r => r.CiudadFinNavigation)
        .FirstOrDefaultAsync(r => r.IdReservacion == id && r.IdUsuario == userId);

    if (reservacion == null)
    {
        return NotFound("Reservación no encontrada para el usuario especificado");
    }

    // Mapear la entidad a DTO para evitar exponer datos sensibles o internos
    var reservacionDTO = new ReservacioneDTO
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
    };

    return Ok(reservacionDTO);
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
                .Include(d => d.CiudadInicio)
                .Include(d => d.CiudadFin)
                .Where(d => d.CarritoId == carrito.id)
                .ToListAsync();

            if (detallesCarrito == null || !detallesCarrito.Any())
            {
                return NotFound("No hay items en el carrito para crear una reservación");
            }

            // Usar fechas normalizadas (sin información de Kind) para PostgreSQL
            var fechaActual = NormalizeDateTime(DateTime.Now);
            var fechaInicio = NormalizeDateTime(detallesCarrito.First().FechaInicio);
            var fechaFin = NormalizeDateTime(detallesCarrito.First().FechaFin);
            
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
                SubTotal = subTotal,
                ciudadinicioid = detallesCarrito.First().CiudadInicioId,
                ciudadfinid = detallesCarrito.First().CiudadFinId
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
                .Include(r => r.CiudadInicioNavigation)
                .Include(r => r.CiudadFinNavigation)
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
                CiudadInicioId = reservacionCreada.ciudadinicioid,
                CiudadFinId = reservacionCreada.ciudadfinid,
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
                    EstadoAsignacion = vr.EstadoAsignacion,
                    ImageUrl = vr.IdVehiculoNavigation.Image_url
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