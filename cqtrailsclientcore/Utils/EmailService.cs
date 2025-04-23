using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace cqtrailsclientcore.Utils;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        
        // Get email settings from environment variables
        _smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") ?? "smtp.gmail.com";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
        _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "your-email@gmail.com";
        _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "your-app-password";
        _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "noreply@cqtrails.com";
        _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "CQTrails";
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            
            message.To.Add(to);

            using var client = new SmtpClient(_smtpServer, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation($"Email sent successfully to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending email: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string newPassword)
    {
        string subject = "CQTrails - Nueva Contraseña";
        string body = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                .content {{ padding: 20px; border: 1px solid #ddd; }}
                .password {{ font-size: 18px; font-weight: bold; background-color: #f5f5f5; padding: 10px; margin: 15px 0; text-align: center; }}
                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #777; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>CQTrails</h1>
                </div>
                <div class='content'>
                    <p>Estimado(a) usuario(a),</p>
                    <p>Recibimos una solicitud para restablecer su contraseña. Hemos generado una nueva contraseña temporal:</p>
                    <div class='password'>{newPassword}</div>
                    <p>Por favor, inicie sesión con esta nueva contraseña y luego cámbiela por una de su preferencia.</p>
                    <p>Si usted no solicitó este cambio, por favor contacte a nuestro equipo de soporte inmediatamente.</p>
                    <p>Gracias,<br>El equipo de CQTrails</p>
                </div>
                <div class='footer'>
                    <p>Este es un correo electrónico automático, por favor no responda a este mensaje.</p>
                </div>
            </div>
        </body>
        </html>";

        await SendEmailAsync(to, subject, body);
    }
} 