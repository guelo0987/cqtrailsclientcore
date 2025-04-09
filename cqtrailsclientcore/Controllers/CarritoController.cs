using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;





[ApiController]
[Route("api/[controller]")]
public class CarritoController:ControllerBase
{


    private readonly MyDbContext _db;
    
    public CarritoController(MyDbContext db)
    {

        _db = db;
    }

    //LoadUser cart    
    [HttpGet("cart/{userid}")]
    public async Task<IActionResult> GetCart(int userid)
    {
        var carritos = await _db.Carrito
            .Include(o => o.Usuario)
            .Where(o => o.usuario_id == userid)
            .ToListAsync();

        if (carritos == null || !carritos.Any())
        {
            return NotFound("No se encontraron carritos para este usuario");
        }

        // Mapear a DTO para evitar exponer datos sensibles
        var carritosDTO = carritos.Select(c => new CarritoDTO
        {
            Id = c.id,
            FechaCreacion = c.fecha_creacion,
            UsuarioId = c.usuario_id,
            Usuario = c.Usuario != null ? new UsuarioBasicoDTO
            {
                IdUsuario = c.Usuario.IdUsuario,
                Nombre = c.Usuario.Nombre,
                Apellido = c.Usuario.Apellido,
                Email = c.Usuario.Email
            } : null
        }).ToList();

        return Ok(carritosDTO);
    }
    
    
    //Get Items in User Cart by User ID
    [HttpGet("user-items/{userid}")]
    public async Task<IActionResult> GetUserCartItems(int userid)
    {
        // Primero obtenemos el carrito del usuario
        var carrito = await _db.Carrito
            .Include(c => c.Usuario)
            .Where(c => c.usuario_id == userid)
            .FirstOrDefaultAsync();

        if (carrito == null)
        {
            return NotFound("El usuario no tiene un carrito");
        }

        // Luego obtenemos los items de ese carrito
        var detalleCarrito = await _db.DetalleCarrito
            .Include(o => o.Vehiculo)
            .Include(o => o.CiudadInicio)
            .Include(o => o.CiudadFin)
            .Where(d => d.CarritoId == carrito.id)
            .ToListAsync();

        if (detalleCarrito == null || !detalleCarrito.Any())
        {
            return NotFound("No se encontraron items en el carrito del usuario");
        }

        // Mapear a DTO para evitar exponer datos sensibles
        var detalleCarritoDTO = detalleCarrito.Select(d => new DetalleCarritoResponseDTO
        {
            Id = d.id,
            CarritoId = d.CarritoId,
            VehiculoId = d.VehiculoId,
            Price = d.Price,
            Cantidad = d.Cantidad,
            FechaInicio = d.FechaInicio,
            FechaFin = d.FechaFin,
            SubTotal = d.SubTotal,
            Total = d.Total,
            CiudadInicioId = d.CiudadInicioId,
            CiudadFinId = d.CiudadFinId,
            Vehiculo = new VehiculoBasicoDTO
            {
                IdVehiculo = d.Vehiculo.IdVehiculo,
                Placa = d.Vehiculo.Placa,
                Modelo = d.Vehiculo.Modelo,
                TipoVehiculo = d.Vehiculo.TipoVehiculo,
                Capacidad = d.Vehiculo.Capacidad,
                Ano = d.Vehiculo.Ano,
                Price = d.Vehiculo.Price
            },
            CiudadInicio = d.CiudadInicio != null ? new CiudadDTO
            {
                IdCiudad = d.CiudadInicio.IdCiudad,
                Nombre = d.CiudadInicio.Nombre,
                Estado = d.CiudadInicio.Estado
            } : null,
            CiudadFin = d.CiudadFin != null ? new CiudadDTO
            {
                IdCiudad = d.CiudadFin.IdCiudad,
                Nombre = d.CiudadFin.Nombre,
                Estado = d.CiudadFin.Estado
            } : null
        }).ToList();

        return Ok(detalleCarritoDTO);
    }
    
    
    
    
    
    
    // Añadir item al carrito del usuario
    [HttpPost("add-item")]
    public async Task<IActionResult> AddItemToCart([FromBody] DetalleCarritoDTO detalleCarritoDto)
    {
        try
        {
            // Verificar si el usuario existe
            var usuario = await _db.Usuarios.FindAsync(detalleCarritoDto.UsuarioId);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Verificar si el vehículo existe
            var vehiculo = await _db.Vehiculos.FindAsync(detalleCarritoDto.VehiculoId);
            if (vehiculo == null)
            {
                return NotFound("Vehículo no encontrado");
            }

            // Buscar el carrito del usuario 
            var carrito = await _db.Carrito
                .Where(c => c.usuario_id == detalleCarritoDto.UsuarioId)
                .FirstOrDefaultAsync();
            
            

            // Verificar si el producto ya existe en el carrito
            var itemExistente = await _db.DetalleCarrito
                .Where(d => d.CarritoId == carrito.id && d.VehiculoId == detalleCarritoDto.VehiculoId)
                .FirstOrDefaultAsync();

            if (itemExistente != null)
            {
                // Actualizar el item existente
                itemExistente.Cantidad = detalleCarritoDto.Cantidad;
                itemExistente.Price = vehiculo.Price;
                itemExistente.FechaInicio = detalleCarritoDto.FechaInicio;
                itemExistente.FechaFin = detalleCarritoDto.FechaFin;
                itemExistente.CiudadInicioId = detalleCarritoDto.CiudadInicioId;
                itemExistente.CiudadFinId = detalleCarritoDto.CiudadFinId;
                itemExistente.SubTotal = vehiculo.Price * detalleCarritoDto.Cantidad;
                itemExistente.Total = itemExistente.SubTotal; // Puedes añadir lógica adicional para impuestos, descuentos, etc.
            }
            else
            {
                // Crear un nuevo detalle de carrito
                var nuevoDetalle = new DetalleCarrito
                {
                    CarritoId = carrito.id,
                    VehiculoId = detalleCarritoDto.VehiculoId,
                    Cantidad = detalleCarritoDto.Cantidad,
                    Price = vehiculo.Price,
                    FechaInicio = detalleCarritoDto.FechaInicio,
                    FechaFin = detalleCarritoDto.FechaFin,
                    CiudadInicioId = detalleCarritoDto.CiudadInicioId,
                    CiudadFinId = detalleCarritoDto.CiudadFinId,
                    SubTotal = vehiculo.Price * detalleCarritoDto.Cantidad,
                    Total = vehiculo.Price * detalleCarritoDto.Cantidad // Puedes añadir lógica adicional para impuestos, descuentos, etc.
                };
                _db.DetalleCarrito.Add(nuevoDetalle);
            }

            await _db.SaveChangesAsync();

            // Obtener los items actualizados del carrito
            var detalleCarritoActualizado = await _db.DetalleCarrito
                .Include(o => o.Vehiculo)
                .Where(d => d.CarritoId == carrito.id)
                .ToListAsync();

            return Ok(detalleCarritoActualizado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    // Aumentar cantidad de un item en el carrito
    [HttpPut("increase-quantity/{detalleId}")]
    public async Task<IActionResult> IncreaseQuantity(int detalleId)
    {
        try
        {
            // Buscar el detalle del carrito
            var detalle = await _db.DetalleCarrito
                .Include(d => d.Vehiculo)
                .FirstOrDefaultAsync(d => d.id == detalleId);

            if (detalle == null)
            {
                return NotFound("Item no encontrado en el carrito");
            }

            // Aumentar la cantidad
            detalle.Cantidad += 1;
            
            // Recalcular subtotal y total
            detalle.SubTotal = detalle.Price * detalle.Cantidad;
            detalle.Total = detalle.SubTotal; // Puedes añadir lógica adicional para impuestos, descuentos, etc.

            await _db.SaveChangesAsync();

            // Obtener los items actualizados del carrito
            var detalleCarritoActualizado = await _db.DetalleCarrito
                .Include(o => o.Vehiculo)
                .Where(d => d.CarritoId == detalle.CarritoId)
                .ToListAsync();

            return Ok(detalleCarritoActualizado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    // Disminuir cantidad de un item en el carrito
    [HttpPut("decrease-quantity/{detalleId}")]
    public async Task<IActionResult> DecreaseQuantity(int detalleId)
    {
        try
        {
            // Buscar el detalle del carrito
            var detalle = await _db.DetalleCarrito
                .Include(d => d.Vehiculo)
                .FirstOrDefaultAsync(d => d.id == detalleId);

            if (detalle == null)
            {
                return NotFound("Item no encontrado en el carrito");
            }

            // Disminuir la cantidad
            if (detalle.Cantidad > 1)
            {
                detalle.Cantidad -= 1;
                
                // Recalcular subtotal y total
                detalle.SubTotal = detalle.Price * detalle.Cantidad;
                detalle.Total = detalle.SubTotal; // Puedes añadir lógica adicional para impuestos, descuentos, etc.

                await _db.SaveChangesAsync();
            }
            else
            {
                // Si la cantidad es 1, se podría eliminar el item o mantenerlo en 1
                // En este caso, lo mantenemos en 1
                return BadRequest("La cantidad mínima es 1");
            }

            // Obtener los items actualizados del carrito
            var detalleCarritoActualizado = await _db.DetalleCarrito
                .Include(o => o.Vehiculo)
                .Where(d => d.CarritoId == detalle.CarritoId)
                .ToListAsync();

            return Ok(detalleCarritoActualizado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
    
    
    
    // Actualizar la fecha de inicio de un item en el carrito
    [HttpPut("update-fecha-inicio/{detalleId}")]
    public async Task<IActionResult> UpdateFechaInicio(int detalleId, [FromBody] UpdateFechaDTO updateFechaDto)
    {
        try
        {
            // Buscar el detalle del carrito por su Id
            var detalle = await _db.DetalleCarrito.FindAsync(detalleId);
            if (detalle == null)
            {
                return NotFound("Detalle del carrito no encontrado.");
            }

            // Actualizar la fecha de inicio con el nuevo valor recibido
            detalle.FechaInicio = updateFechaDto.NewFecha;
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Fecha de inicio actualizada exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

// Actualizar la fecha de fin de un item en el carrito
    [HttpPut("update-fecha-fin/{detalleId}")]
    public async Task<IActionResult> UpdateFechaFin(int detalleId, [FromBody] UpdateFechaDTO updateFechaDto)
    {
        try
        {
            // Buscar el detalle del carrito por su Id
            var detalle = await _db.DetalleCarrito.FindAsync(detalleId);
            if (detalle == null)
            {
                return NotFound("Detalle del carrito no encontrado.");
            }

            // Actualizar la fecha de fin con el nuevo valor recibido
            detalle.FechaFin = updateFechaDto.NewFecha;
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Fecha de fin actualizada exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }


    // Eliminar un item del carrito
    [HttpDelete("remove-item/{detalleId}")]
    public async Task<IActionResult> RemoveItemFromCart(int detalleId)
    {
        try
        {
            // Buscar el detalle del carrito
            var detalle = await _db.DetalleCarrito.FindAsync(detalleId);

            if (detalle == null)
            {
                return NotFound("Item no encontrado en el carrito");
            }

            // Guardar el ID del carrito antes de eliminar el detalle
            int carritoId = detalle.CarritoId;

            // Eliminar el detalle
            _db.DetalleCarrito.Remove(detalle);
            await _db.SaveChangesAsync();

            // Obtener los items actualizados del carrito
            var detalleCarritoActualizado = await _db.DetalleCarrito
                .Include(o => o.Vehiculo)
                .Where(d => d.CarritoId == carritoId)
                .ToListAsync();

            return Ok(detalleCarritoActualizado);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    // Vaciar completamente el carrito de un usuario
    [HttpDelete("clear-cart/{userId}")]
    public async Task<IActionResult> ClearCart(int userId)
    {
        try
        {
            // Buscar el carrito del usuario
            var carrito = await _db.Carrito
                .Where(c => c.usuario_id == userId)
                .FirstOrDefaultAsync();

            if (carrito == null)
            {
                return NotFound("Carrito no encontrado");
            }

            // Obtener todos los detalles del carrito
            var detalles = await _db.DetalleCarrito
                .Where(d => d.CarritoId == carrito.id)
                .ToListAsync();

            // Eliminar todos los detalles
            _db.DetalleCarrito.RemoveRange(detalles);
            await _db.SaveChangesAsync();

            return Ok("Carrito vaciado correctamente");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}