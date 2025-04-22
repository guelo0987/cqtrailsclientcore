using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
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
    // Validar que no exista el usuario
    if (_db.Usuarios.Any(u => u.Email == registerRequest.Email))
    {
        return Conflict("El correo ya está registrado");
    }

    var rol = _db.Roles.FirstOrDefault(r => r.NombreRol == "Cliente");
    if (rol == null)
    {
        return StatusCode(500, "Error en la configuración del servidor");
    }
    
    // Crear y guardar el usuario
    var usuario = new Usuario()
    {
        Nombre = registerRequest.Nombre,
        Apellido = registerRequest.Apellido,
        Email = registerRequest.Email,
        PasswordHash = registerRequest.PasswordHash, 
        IdRol = 1, // Verifica que el IdRol sea correcto o asigna el id de "Cliente"
        Activo = true
    };

    _db.Usuarios.Add(usuario);
    await _db.SaveChangesAsync(); // Guarda el usuario y genera el IdUsuario

    // Crear el carrito usando el Id del usuario
    var carrito = new Carrito()
    {
        fecha_creacion = DateTime.UtcNow,
        usuario_id = usuario.IdUsuario // Se asigna el ID generado al usuario
    };

    _db.Carrito.Add(carrito);
    await _db.SaveChangesAsync();

    // Crear la empresa asociada a este registro
    var empresa = new Empresa()
    {
        Nombre = registerRequest.NombreEmpresa,
        ContactoEmail = registerRequest.ContactoEmail,
        ContactoTelefono = registerRequest.ContactoTelefono,
        FechaRegistro = DateTime.UtcNow, // Se asigna la fecha actual
        Activo = true
    };

    _db.Empresas.Add(empresa);
    await _db.SaveChangesAsync();

    // Resto del código para la generación del token
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, Environment.GetEnvironmentVariable("JWT_SUBJECT")),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("Id", usuario.IdUsuario.ToString()),
        new Claim(ClaimTypes.Role, rol.NombreRol)
    };

    // Opcionalmente se puede devolver también la información de la empresa
    var usuarioResponse = new UsuarioDTO()
    {
        Nombre = registerRequest.Nombre,
        Apellido = registerRequest.Apellido,
        Email = registerRequest.Email
    };

    return GenerateToken(claims, usuarioResponse, rol.NombreRol);
}

[HttpPut("update-user-and-empresa")]
public async Task<IActionResult> UpdateUserAndEmpresa([FromBody] UpdateUserAndEmpresaDTO updateDto)
{
    // Buscar el usuario por su correo actual
    var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == updateDto.CurrentEmail);
    if (user == null)
    {
        return NotFound("Usuario no encontrado con el correo proporcionado.");
    }

    // Buscar la empresa asociada al correo actual del usuario
    var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.ContactoEmail == updateDto.CurrentEmail);
    if (empresa == null)
    {
        return NotFound("No se encontró una empresa asociada al usuario.");
    }

    // Actualizar la información del usuario
    user.Nombre = updateDto.NewNombre;
    user.Apellido = updateDto.NewApellido;
    user.Email = updateDto.NewEmail;

    // Actualizar la información de la empresa
    empresa.Nombre = updateDto.NewEmpresaNombre ?? empresa.Nombre; // Solo actualizar si se proporciona un nuevo nombre
    empresa.ContactoTelefono = updateDto.NewEmpresaTelefono ?? empresa.ContactoTelefono; // Solo actualizar si se proporciona un nuevo teléfono

    // Actualizar el ContactoEmail de la empresa si el correo del usuario cambia
    if (updateDto.CurrentEmail != updateDto.NewEmail)
    {
        empresa.ContactoEmail = updateDto.NewEmail;
    }

    // Guardar los cambios en la base de datos
    await _db.SaveChangesAsync();

    return Ok(new { Message = "Información del usuario y la empresa actualizada exitosamente." });
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