using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace cqtrailsclientcore.Controllers;



[ApiController]
[Route("api/[controller]")]
public class AuthController:ControllerBase
{


    private readonly MyDbContext _db;
    
    public AuthController(MyDbContext db)
    {
        _db = db;

    }
    
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequest)
    {
        var client = await _db.Usuarios
            .Include(o => o.IdRolNavigation)
            .FirstOrDefaultAsync(o => o.Email == loginRequest.email);

        if (client == null)
        {
            return NotFound("Usuario no Encontrado");
        }

        if (loginRequest.password != client.PasswordHash)
        {
            return Unauthorized("Credenciales Invalidas");
        }

        if (client.IdRolNavigation.NombreRol != "Cliente")
        {
            return Unauthorized("Rol no valido");
        }

        // Crear los claims para el token JWT
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Environment.GetEnvironmentVariable("JWT_SUBJECT")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("Id", client.IdUsuario.ToString()),
            new Claim(ClaimTypes.Role, client.IdRolNavigation.NombreRol)
        };

        

        return GenerateToken(claims, new UsuarioDTO
        {
            
           Nombre = client.Nombre,
            Email = client.Email,
            Apellido = client.Apellido,
            
        }, client.IdRolNavigation.NombreRol);
    }
    
    
    
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UsuarioDTO registerRequest)
    {
        if (_db.Usuarios.Any(u => u.Email == registerRequest.Email))
        {
            return Conflict("El correo ya está registrado");
        }

        var rol = _db.Roles.FirstOrDefault(r => r.NombreRol == "Cliente");
        if (rol == null)
        {
            return StatusCode(500, "Error en la configuración del servidor");
        }
    
        var usuario = new Usuario()
        {
            Nombre = registerRequest.Nombre,
            Email = registerRequest.Email,
            PasswordHash = registerRequest.PasswordHash, 
            Apellido = registerRequest.Apellido,
            IdRol = 1,
            Activo = true
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(); // Guarda el usuario para obtener su ID

        // Crear el carrito con el ID del usuario
        var carrito = new Carrito()
        {
            fecha_creacion = DateTime.UtcNow,
            usuario_id = usuario.IdUsuario // Asigna el ID generado
        };

        _db.Carrito.Add(carrito); // Añade el carrito al contexto
        await _db.SaveChangesAsync(); // Guarda el carrito

        // Resto del código para generar el token...
    
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Environment.GetEnvironmentVariable("JWT_SUBJECT")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("Id", usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Role, rol.NombreRol)
        };

        var usuarioDTO = new UsuarioDTO()
        {
            Nombre = registerRequest.Nombre,
            Email = registerRequest.Email,
            Apellido = registerRequest.Apellido,
        };

        return GenerateToken(claims, usuarioDTO, rol.NombreRol);
    }








    
    
    private IActionResult GenerateToken(Claim[] claims, UsuarioDTO userClienteDto, string role)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                     throw new InvalidOperationException("JWT_KEY not found in environment variables");
            
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
            audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: signIn
        );

        string tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        

        return Ok(new { Token = tokenValue, User = userClienteDto, Role = role });
    }
    
    
}