using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MedCenter.Services.Authentication.Components
{
    public class SecretariaAuthenticator : IAuthComponent
    {
        private AppDbContext _context;
        private PasswordHashService _hashService;
        private RoleKeyValidationService _roleKeyService;

        public SecretariaAuthenticator(AppDbContext appDbContext, PasswordHashService passwordHashService, RoleKeyValidationService roleKeyValidationService)
        {
            _context = appDbContext;
            _hashService = passwordHashService;
            _roleKeyService = roleKeyValidationService;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {

            var secretaria = await _context.secretarias.Include(s => s.idNavigation).FirstOrDefaultAsync(s => s.idNavigation.email == username);

            if (secretaria != null)
            {
                if (_hashService.VerifyPassword(password, secretaria.idNavigation.contraseña))
                    return new AuthResult { Success = true, Role = RolUsuario.Secretaria, UserName = secretaria.idNavigation.nombre, UserId = secretaria.idNavigation.id, UserMail = secretaria.idNavigation.email };
                else
                    return new AuthResult { Success = false, ErrorMessage = "La contraseña es incorrecta" };
            }
            else
                return new AuthResult { Success = false, ErrorMessage = "No se ha encontrado una cuenta de secretaria con el mail proporcionado" };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO dto)
        {
            if (dto.Role != "Secretaria")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };
            // Verificar si ya existe el email
            var emailExists = await _context.personas.AnyAsync(p => p.email == dto.Email);
            if (emailExists)
                return new AuthResult { Success = false, ErrorMessage = "El email ya está registrado" };

            if (string.IsNullOrEmpty(dto.ClaveSecretaria))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetKeyRequiredMessage("Secretaria") };
            if (!await _roleKeyService.ValidateRoleKeyAsync(dto.ClaveSecretaria, "Secretaria"))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetWrongKeyMessage("Secretaria") };

            var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Persona persona = new Persona
                {
                    nombre = dto.Nombre,
                    email = dto.Email,
                    contraseña = _hashService.HashPassword(dto.Password)
                };

                _context.personas.Add(persona);
                await _context.SaveChangesAsync();//Hago el save aca para que se asigne el Id de forma automatica en la db y poder darselo despues a secretaria

                Secretaria secretaria = new Secretaria
                {
                    id = persona.id,
                    legajo = dto.Legajo,
                    idNavigation = persona
                };

                _context.secretarias.Add(secretaria);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new AuthResult { Success = true, Role = RolUsuario.Secretaria, UserId = persona.id, UserName = persona.nombre, UserMail = persona.email };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new AuthResult { Success = false, ErrorMessage = "Error interno del servidor al crear la secretaria" };
            }
        }
    }
}