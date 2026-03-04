using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MedCenter.Services.EmailService;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendRecoveryCodeEmailAsync(string toEmail, string userName, string code)
    {
        try
        {
            var message = new MimeMessage();

            // Sender (from configuration)
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "lautaberca2005@gmail.com";
            var fromName = _configuration["EmailSettings:FromName"] ?? "MedCenter";
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            // Recipient
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Código de Recuperación - MedCenter";

            // HTML Body with styling
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                        <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h2 style='color: #667eea; margin: 0;'>🔐 Recuperación de Contraseña</h2>
                            </div>
                            
                            <p style='color: #333; font-size: 16px;'>Hola <strong>{userName}</strong>,</p>
                            <p style='color: #666;'>Recibimos una solicitud para restablecer tu contraseña en MedCenter.</p>
                            
                            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px; margin: 30px 0; text-align: center;'>
                                <p style='margin: 0; color: white; font-size: 14px;'>Tu código de recuperación es:</p>
                                <h1 style='color: white; font-size: 48px; letter-spacing: 8px; margin: 15px 0; font-family: monospace;'>
                                    {code}
                                </h1>
                            </div>
                            
                            <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #856404;'><strong>⏱️ Este código expira en 10 minutos</strong></p>
                                <p style='margin: 5px 0 0 0; color: #856404;'>📱 Tienes 3 intentos para ingresar el código correctamente</p>
                            </div>
                            
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                                <p style='color: #999; font-size: 14px; margin: 0;'>
                                    ℹ️ Si no solicitaste este código, ignora este mensaje. Tu contraseña permanecerá sin cambios.
                                </p>
                            </div>
                            
                            <div style='text-align: center; margin-top: 30px; color: #999; font-size: 12px;'>
                                <p>© {DateTime.Now.Year} MedCenter - Sistema de Gestión Médica</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                TextBody = $@"
Recuperación de Contraseña - MedCenter

Hola {userName},

Recibimos una solicitud para restablecer tu contraseña.

Tu código de recuperación es: {code}

⏱️ Este código expira en 10 minutos
📱 Tienes 3 intentos para ingresar el código correctamente

Si no solicitaste este código, ignora este mensaje.

© {DateTime.Now.Year} MedCenter
                "
            };

            message.Body = bodyBuilder.ToMessageBody();

            // SMTP Configuration (from appsettings.json)
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? fromEmail;
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

            if (string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("SMTP password not configured in appsettings.json");
                return false;
            }

            // Send email
            using (var client = new SmtpClient())
            {
                // Connect to SMTP server
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);

                // Authenticate
                await client.AuthenticateAsync(smtpUsername, smtpPassword);

                // Send message
                await client.SendAsync(message);

                // Disconnect
                await client.DisconnectAsync(true);
            }

            _logger.LogInformation($"Recovery code email sent successfully to: {toEmail}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send recovery email to {toEmail}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a cancellation notification email to a patient when their appointment is canceled
    /// by a secretaria or by force-deactivation of an account.
    /// </summary>
    public async Task<bool> SendTurnoCanceladoEmailAsync(
        string toEmail, string pacienteNombre, string medicoNombre,
        string especialidad, DateOnly fecha, TimeOnly hora, string motivo)
    {
        try
        {
            var message = new MimeMessage();
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "lautaberca2005@gmail.com";
            var fromName = _configuration["EmailSettings:FromName"] ?? "MedCenter";
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(pacienteNombre, toEmail));
            message.Subject = "Turno Cancelado - MedCenter";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                        <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h2 style='color: #ef4444; margin: 0;'>❌ Turno Cancelado</h2>
                            </div>
                            
                            <p style='color: #333; font-size: 16px;'>Hola <strong>{pacienteNombre}</strong>,</p>
                            <p style='color: #666;'>Le informamos que su turno ha sido cancelado.</p>
                            
                            <div style='background: #fef2f2; border-left: 4px solid #ef4444; padding: 20px; margin: 20px 0; border-radius: 0 8px 8px 0;'>
                                <p style='margin: 5px 0; color: #333;'><strong>📅 Fecha:</strong> {fecha:dd/MM/yyyy}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>🕐 Hora:</strong> {hora:HH:mm}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>👨‍⚕️ Médico:</strong> {medicoNombre}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>🏥 Especialidad:</strong> {especialidad}</p>
                            </div>

                            <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #856404;'><strong>Motivo:</strong> {motivo}</p>
                            </div>
                            
                            <p style='color: #666;'>Por favor, ingrese al sistema para reprogramar su turno si lo necesita.</p>
                            
                            <div style='text-align: center; margin-top: 30px; color: #999; font-size: 12px;'>
                                <p>© {DateTime.Now.Year} MedCenter - Sistema de Gestión Médica</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                TextBody = $@"
Turno Cancelado - MedCenter

Hola {pacienteNombre},

Le informamos que su turno ha sido cancelado.

Fecha: {fecha:dd/MM/yyyy}
Hora: {hora:HH:mm}
Médico: {medicoNombre}
Especialidad: {especialidad}
Motivo: {motivo}

Por favor, ingrese al sistema para reprogramar su turno si lo necesita.

© {DateTime.Now.Year} MedCenter
                "
            };

            message.Body = bodyBuilder.ToMessageBody();
            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send cancellation email to {toEmail}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a reservation confirmation email to a patient when an appointment is booked.
    /// </summary>
    public async Task<bool> SendTurnoReservadoEmailAsync(
        string toEmail, string pacienteNombre, string medicoNombre,
        string especialidad, DateOnly fecha, TimeOnly hora)
    {
        try
        {
            var message = new MimeMessage();
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "lautaberca2005@gmail.com";
            var fromName = _configuration["EmailSettings:FromName"] ?? "MedCenter";
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(pacienteNombre, toEmail));
            message.Subject = "Confirmación de Turno - MedCenter";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                        <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h2 style='color: #10b981; margin: 0;'>✅ Turno Reservado</h2>
                            </div>
                            
                            <p style='color: #333; font-size: 16px;'>Hola <strong>{pacienteNombre}</strong>,</p>
                            <p style='color: #666;'>Su turno ha sido reservado exitosamente.</p>
                            
                            <div style='background: #ecfdf5; border-left: 4px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 0 8px 8px 0;'>
                                <p style='margin: 5px 0; color: #333;'><strong>📅 Fecha:</strong> {fecha:dd/MM/yyyy}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>🕐 Hora:</strong> {hora:HH:mm}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>👨‍⚕️ Médico:</strong> {medicoNombre}</p>
                                <p style='margin: 5px 0; color: #333;'><strong>🏥 Especialidad:</strong> {especialidad}</p>
                            </div>

                            <div style='background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0;'>
                                <p style='margin: 0; color: #1e40af;'><strong>📌 Recuerde:</strong> Si necesita cancelar o reprogramar, hágalo con al menos 24 horas de anticipación.</p>
                            </div>
                            
                            <div style='text-align: center; margin-top: 30px; color: #999; font-size: 12px;'>
                                <p>© {DateTime.Now.Year} MedCenter - Sistema de Gestión Médica</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ",
                TextBody = $@"
Confirmación de Turno - MedCenter

Hola {pacienteNombre},

Su turno ha sido reservado exitosamente.

Fecha: {fecha:dd/MM/yyyy}
Hora: {hora:HH:mm}
Médico: {medicoNombre}
Especialidad: {especialidad}

Recuerde: Si necesita cancelar o reprogramar, hágalo con al menos 24 horas de anticipación.

© {DateTime.Now.Year} MedCenter
                "
            };

            message.Body = bodyBuilder.ToMessageBody();
            return await SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send reservation email to {toEmail}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Private helper to send an already-built MimeMessage via SMTP.
    /// </summary>
    private async Task<bool> SendEmailAsync(MimeMessage message)
    {
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "lautaberca2005@gmail.com";
        var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? fromEmail;
        var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

        if (string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogError("SMTP password not configured in appsettings.json");
            return false;
        }

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        return true;
    }
}