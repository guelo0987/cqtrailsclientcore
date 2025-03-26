using System;
using System.Linq;
using System.Threading.Tasks;
using cqtrailsclientcore.Context;
using cqtrailsclientcore.DTO;
using cqtrailsclientcore.Models;
using cqtrailsclientcore.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserRecoveryController : ControllerBase
{
    private readonly MyDbContext _db;

    public UserRecoveryController(MyDbContext db)
    {
        _db = db;
    }

    // DTO para solicitud de recuperación de contraseña
    public class RecoveryRequestDTO
    {
        public string Email { get; set; }
    }

    // DTO para cambio de contraseña
    public class ChangePasswordDTO
    {
        public string Email { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    // DTO para respuesta de recuperación
    public class RecoveryResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    // POST: api/UserRecovery/recover
    [HttpPost("recover")]
    public async Task<ActionResult<RecoveryResponseDTO>> RecoverPassword([FromBody] RecoveryRequestDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new RecoveryResponseDTO
                {
                    Success = false,
                    Message = "El correo electrónico es requerido"
                });
            }

            var user = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Por seguridad, no revelamos si el correo existe o no
                return Ok(new RecoveryResponseDTO
                {
                    Success = true,
                    Message = "Si el correo existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña"
                });
            }

            // Aquí normalmente enviarías un correo con un token o enlace para restablecer la contraseña
            // Por ahora, solo simulamos el proceso

            // En una implementación real:
            // 1. Generar un token único
            // 2. Almacenar el token en la base de datos con una fecha de expiración
            // 3. Enviar un correo con un enlace que incluya el token

            return Ok(new RecoveryResponseDTO
            {
                Success = true,
                Message = "Si el correo existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RecoveryResponseDTO
            {
                Success = false,
                Message = $"Error interno del servidor: {ex.Message}"
            });
        }
    }

    // POST: api/UserRecovery/change-password
    [HttpPost("change-password")]
    [Authorize] // Requiere autenticación
    public async Task<ActionResult<RecoveryResponseDTO>> ChangePassword([FromBody] ChangePasswordDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email) || 
                string.IsNullOrEmpty(request.OldPassword) || 
                string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new RecoveryResponseDTO
                {
                    Success = false,
                    Message = "Todos los campos son requeridos"
                });
            }

            var user = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new RecoveryResponseDTO
                {
                    Success = false,
                    Message = "Usuario no encontrado"
                });
            }

            // Verificar la contraseña anterior
            // Nota: En un sistema real, deberías usar hash para las contraseñas
            if (user.PasswordHash != request.OldPassword)
            {
                return Unauthorized(new RecoveryResponseDTO
                {
                    Success = false,
                    Message = "La contraseña actual es incorrecta"
                });
            }

            // Actualizar la contraseña
            user.PasswordHash = request.NewPassword;
            
            // En un sistema real, deberías hashear la nueva contraseña:
            // user.PasswordHash = PassHasher.HashPassword(request.NewPassword);

            await _db.SaveChangesAsync();

            return Ok(new RecoveryResponseDTO
            {
                Success = true,
                Message = "Contraseña actualizada correctamente"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RecoveryResponseDTO
            {
                Success = false,
                Message = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}