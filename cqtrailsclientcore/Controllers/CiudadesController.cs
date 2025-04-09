using cqtrailsclientcore.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;


[ApiController]
[Route("api/[controller]")]
public class CiudadesController:ControllerBase
{
    
    private readonly MyDbContext _db;
    
    public CiudadesController(MyDbContext db)
    {
        _db = db;

    }
    
    [HttpGet]
    public async Task<IActionResult> GetCiudades()
    {
        var ciudades = await _db.Ciudades.ToListAsync();
        return Ok(ciudades);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCiudad(int id)
    {
        var ciudad = await _db.Ciudades.FindAsync(id);
        if (ciudad == null)
        {
            return NotFound();
        }
        return Ok(ciudad);
    }
    
}