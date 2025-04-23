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
using Microsoft.Extensions.Logging;

namespace cqtrailsclientcore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserRecoveryController : ControllerBase
{
    private readonly MyDbContext _db;
    private readonly EmailService _emailService;
    private readonly ILogger<UserRecoveryController> _logger;

    public UserRecoveryController(MyDbContext db, ILogger<UserRecoveryController> logger, EmailService emailService)
    {
        _db = db;
        _logger = logger;
        _emailService = emailService;
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
    
    
    
    
    // Update User Information
    //PUT: api/UserInformation Update/update
    [HttpPut("update")]
    
    

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

            // Generar una nueva contraseña temporal
            string newPassword = PasswordGenerator.GenerateRandomPassword(12, true);
            
            // Hashear y guardar la nueva contraseña en la base de datos
            user.PasswordHash = PassHasher.HashPassword(newPassword);
            await _db.SaveChangesAsync();
            
            try
            {
                // Enviar correo con la nueva contraseña
                await _emailService.SendPasswordResetEmailAsync(user.Email, newPassword);
                _logger.LogInformation($"Password reset email sent to {user.Email}");
            }
            catch (Exception emailEx)
            {
                _logger.LogError($"Failed to send password reset email: {emailEx.Message}");
                // Continuamos con la operación a pesar del error en el envío de correo
            }

            return Ok(new RecoveryResponseDTO
            {
                Success = true,
                Message = "Si el correo existe en nuestro sistema, recibirás instrucciones para recuperar tu contraseña"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Password recovery error: {ex.Message}");
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
            if (!PassHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
            {
                return Unauthorized(new RecoveryResponseDTO
                {
                    Success = false,
                    Message = "La contraseña actual es incorrecta"
                });
            }

            // Actualizar la contraseña
            user.PasswordHash = PassHasher.HashPassword(request.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new RecoveryResponseDTO
            {
                Success = true,
                Message = "Contraseña actualizada correctamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Change password error: {ex.Message}");
            return StatusCode(500, new RecoveryResponseDTO
            {
                Success = false,
                Message = $"Error interno del servidor: {ex.Message}"
            });
        }
    }
}