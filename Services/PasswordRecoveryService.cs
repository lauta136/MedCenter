using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MedCenter.Services.Authentication.Components;

namespace MedCenter.Services
{
    public class PasswordRecoveryService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly EmailService.EmailService _emailService;
        private readonly ILogger<PasswordRecoveryService> _logger;
        private readonly PasswordHashService _hashService;

        public PasswordRecoveryService(
            AppDbContext context,
            IMemoryCache cache,
            EmailService.EmailService emailService,
            ILogger<PasswordRecoveryService> logger,
            PasswordHashService hashService)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _logger = logger;
            _hashService = hashService;
        }

        public async Task<bool> SendRecoveryCodeAsync(string email)
        {
            var usuario = await _context.personas
                .FirstOrDefaultAsync(p => p.email == email);

            if (usuario == null)
            {
                _logger.LogWarning($"Recovery code requested for non-existent email: {email}");
                // Return true anyway (security: don't reveal if user exists)
                return true;
            }

            // Generate 6-digit code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            // Store in cache (expires in 10 minutes)
            var cacheKey = $"password-reset:{email.ToLower()}";
            var resetData = new
            {
                UserId = usuario.id,
                Email = email,
                Code = code,
                Expiracion = DateTime.UtcNow.AddMinutes(10),
                IntentosRestantes = 3
            };

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            _cache.Set(cacheKey, resetData, options);

            // Send email with code
            try
            {
                await _emailService.SendRecoveryCodeEmailAsync(usuario.email!, usuario.nombre ?? "Usuario", code);
                _logger.LogInformation($"Recovery code sent to: {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send recovery email to {email}: {ex.Message}");
                // Remove cache entry if email fails
                _cache.Remove(cacheKey);
                return false;
            }
        }

        public bool ValidateRecoveryCode(string email, string code, out int userId, out string? errorMessage)
        {
            userId = 0;
            errorMessage = null;
            var cacheKey = $"password-reset:{email.ToLower()}";

            if (!_cache.TryGetValue(cacheKey, out dynamic? resetData) || resetData == null)
            {
                _logger.LogWarning($"No recovery code found for email: {email}");
                errorMessage = "Código expirado o no válido. Solicita uno nuevo.";
                return false;
            }

            // Check expiration
            DateTime expiracion = resetData.Expiracion;
            if (DateTime.UtcNow > expiracion)
            {
                _cache.Remove(cacheKey);
                _logger.LogWarning($"Expired recovery code for email: {email}");
                errorMessage = "El código ha expirado. Solicita uno nuevo.";
                return false;
            }

            // Check remaining attempts
            if (resetData.IntentosRestantes <= 0)
            {
                _cache.Remove(cacheKey);
                _logger.LogWarning($"Too many failed attempts for email: {email}");
                errorMessage = "Demasiados intentos fallidos. Solicita un nuevo código.";
                return false;
            }

            // Validate code
            if (resetData.Code != code)
            {
                // Decrement attempts
                var updatedData = new
                {
                    UserId = (int)resetData.UserId,
                    Email = (string)resetData.Email,
                    Code = (string)resetData.Code,
                    Expiracion = (DateTime)resetData.Expiracion,
                    IntentosRestantes = (int)resetData.IntentosRestantes - 1
                };
                
                _cache.Set(cacheKey, updatedData, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
                
                _logger.LogWarning($"Invalid code attempt for email: {email}. Attempts remaining: {updatedData.IntentosRestantes}");
                errorMessage = $"Código incorrecto. Te quedan {updatedData.IntentosRestantes} intento(s).";
                return false;
            }

            userId = resetData.UserId;
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            if (!ValidateRecoveryCode(email, code, out int userId, out string? errorMessage))
            {
                _logger.LogWarning($"Password reset failed for {email}: {errorMessage}");
                return false;
            }

            var usuario = await _context.personas.FindAsync(userId);
            if (usuario == null)
            {
                _logger.LogError($"User not found for userId: {userId}");
                return false;
            }

            // Hash and save new password
            usuario.contraseña = _hashService.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Remove code from cache (one-time use)
            var cacheKey = $"password-reset:{email.ToLower()}";
            _cache.Remove(cacheKey);

            _logger.LogInformation($"Password successfully reset for user: {usuario.nombre}");
            return true;
        }

        public string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            var username = parts[0];
            var domain = parts[1];

            if (username.Length <= 3)
                return $"{username[0]}***@{domain}";

            return $"{username.Substring(0, 3)}***@{domain}";
        }
    }
}