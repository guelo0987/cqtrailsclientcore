using cqtrailsclientcore.Context;
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
            .FirstOrDefaultAsync(o => o.IdReservacion == idreservacion && o.IdUsuario == userid);

        if (reservacion == null)
            return BadRequest("Reservación no encontrada");

        if (reservacion.Estado != "Aceptada")
            return BadRequest("La reservación no está aprobada");

        // Verificar si ya existe una prefactura
        var prefacturaExistente = await _db.PreFacturas
            .Include(p => p.IdReservacionNavigation)
            .ThenInclude(r => r.IdUsuarioNavigation)
            .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);

        if (prefacturaExistente != null)
            return Ok(prefacturaExistente);

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
                .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);
            
            return prefacturaCreada != null 
                ? Ok(prefacturaCreada) 
                : StatusCode(500, "Error al generar la prefactura");
        }

        // Recargar la entidad con las relaciones
        var prefacturaActualizada = await _db.PreFacturas
            .Include(p => p.IdReservacionNavigation)
            .ThenInclude(r => r.IdUsuarioNavigation)
            .FirstOrDefaultAsync(p => p.IdReservacion == idreservacion);

        return Ok(prefacturaActualizada);
    }
}