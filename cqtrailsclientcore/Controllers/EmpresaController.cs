using cqtrailsclientcore.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;




[ApiController]
[Route("api/[controller]")]
public class EmpresaController:ControllerBase
{
    
    private readonly MyDbContext _db;
    
    public EmpresaController(MyDbContext db)
    {
        _db = db;

    }
    
    [HttpGet]
    public async Task<IActionResult> GetEmpresas()
    {
        var empresas = await _db.Empresas.ToListAsync();
        return Ok(empresas);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmpresa(int id)
    {
        var empresa = await _db.Empresas.FindAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }
        return Ok(empresa);
    }
    
    
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetEmpresaByEmail(string email)
    {
        var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.ContactoEmail == email);
        if (empresa == null)
        {
            return NotFound();
        }
        return Ok(empresa);
    }
    
    
    
}