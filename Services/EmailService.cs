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
}