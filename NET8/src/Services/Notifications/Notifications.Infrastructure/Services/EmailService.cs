using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.Application.Interfaces;
using Notifications.Application.Settings;
using Shared.Contracts.Messages;

namespace Notifications.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendNotificationEmailAsync(NotificationMessage message)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(new MailboxAddress(message.Usuario, message.Email));
        email.Subject = $"Carga Masiva Completada - {message.NombreArchivo}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GenerateHtmlBody(message),
            TextBody = GenerateTextBody(message)
        };

        email.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();

            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await smtp.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, secureSocketOptions);

            if (!string.IsNullOrEmpty(_settings.SenderPassword))
            {
                await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
            }

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Correo enviado exitosamente a {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo a {Email}", message.Email);
            throw;
        }
    }

    private static string GenerateHtmlBody(NotificationMessage message)
    {
        var status = message.TotalRegistrosFallidos == 0 ? "✅ Exitosa" : "⚠️ Con errores";

        return $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .stats {{ margin: 20px 0; }}
        .stat-item {{ padding: 10px; margin: 5px 0; background-color: white; border-radius: 5px; }}
        .success {{ color: #4CAF50; }}
        .warning {{ color: #ff9800; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔔 Carga Masiva Completada</h1>
        </div>
        <div class=""content"">
            <p>Hola <strong>{message.Usuario}</strong>,</p>
            <p>Tu carga masiva ha sido procesada con el siguiente resultado:</p>

            <div class=""stats"">
                <div class=""stat-item"">
                    <strong>Archivo:</strong> {message.NombreArchivo}
                </div>
                <div class=""stat-item"">
                    <strong>Estado:</strong> {status}
                </div>
                <div class=""stat-item success"">
                    <strong>Registros Procesados:</strong> {message.TotalRegistrosProcesados}
                </div>
                <div class=""stat-item warning"">
                    <strong>Registros Fallidos:</strong> {message.TotalRegistrosFallidos}
                </div>
                <div class=""stat-item"">
                    <strong>Fecha de Finalización:</strong> {message.FechaFin:dd/MM/yyyy HH:mm:ss}
                </div>
            </div>

            <p>Puedes consultar el detalle de la carga en el sistema.</p>
        </div>
        <div class=""footer"">
            <p>Este es un correo automático, por favor no responder.</p>
            <p>Sistema de Carga Masiva © {DateTime.Now.Year}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateTextBody(NotificationMessage message)
    {
        return $"""
            Carga Masiva Completada
            =======================
            
            Hola {message.Usuario},
            
            Tu carga masiva ha sido procesada:
            
            - Archivo: {message.NombreArchivo}
            - Registros Procesados: {message.TotalRegistrosProcesados}
            - Registros Fallidos: {message.TotalRegistrosFallidos}
            - Fecha de Finalización: {message.FechaFin:dd/MM/yyyy HH:mm:ss}
            
            Puedes consultar el detalle de la carga en el sistema.
            
            --
            Sistema de Carga Masiva
            """;
    }
}
