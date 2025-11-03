using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedCenter.Services.Authentication.Components
{

    public class RoleKeyValidationService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHashService _passwordHashService;

        public RoleKeyValidationService(AppDbContext context, PasswordHashService passwordHashService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
        }

        /// <summary>
        /// Valida la clave de rol usando las claves almacenadas en la base de datos
        /// </summary>
        public async Task<bool> ValidateRoleKeyAsync(string inputKey, string role)
        {
            if (string.IsNullOrWhiteSpace(inputKey) || string.IsNullOrWhiteSpace(role))
                return false;

            try
            {
                // Buscar la clave hasheada en la base de datos para el rol específico
                var storedHashedKey = await _context.role_keys
                    .Where(r => r.Role.ToLower() == role.ToLower())
                    .Select(r => r.HashedKey)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(storedHashedKey))
                    return false;

                // Verificar la clave usando el servicio de hash
                return _passwordHashService.VerifyPassword(inputKey, storedHashedKey);
            }
            catch (Exception)
            {
                // Log del error en un sistema real
                return false;
            }
        }

        /// <summary>
        /// Obtiene el mensaje de error personalizado según el rol
        /// </summary>
        public string GetKeyRequiredMessage(string role)
        {
            return role?.ToLower() switch
            {
                "secretaria" => "Se requiere clave de autorización para registrar personal de secretaría",
                "medico" => "Se requiere clave de autorización para registrar médicos",
                _ => "Se requiere clave de autorización para este rol"
            };
        }

        public string GetWrongKeyMessage(string role)
        {
            return role?.ToLower() switch
            {
                "secretaria" => "La clave del rol secretaria insertada es erronea",
                "medico" => "La clave del rol medico insertada es erronea",
                _ => "Se requiere clave de autorización para este rol"
            };
        }

        /// <summary>
        /// Verifica si un rol requiere clave de autorización
        /// </summary>
        public async Task<bool> RoleRequiresKeyAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return await _context.role_keys
                .AnyAsync(r => r.Role.ToLower() == role.ToLower());
        }
    }
}