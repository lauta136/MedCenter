using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Model;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.Authentication.Components
{
    public class AdminAuthenticator : IAuthComponent
    {
        private AppDbContext _context;
        private PasswordHashService _hashService;
        private RoleKeyValidationService _roleKeyService;

        public AdminAuthenticator(AppDbContext appDbContext, PasswordHashService passwordHashService, RoleKeyValidationService roleKeyValidationService)
        {
            _context = appDbContext;
            _hashService = passwordHashService;
            _roleKeyService = roleKeyValidationService;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            // Admin es una persona sin rol específico (sin Medico, Secretaria, ni Paciente)
            var admin = await _context.admins
                .Include(a => a.IdNavigation)
                .FirstOrDefaultAsync(p => p.IdNavigation.email == username);

            if (admin != null)
            {
                if (_hashService.VerifyPassword(password, admin.IdNavigation.contraseña))
                    return new AuthResult 
                    { 
                        Success = true, 
                        Role = RolUsuario.Admin, 
                        UserName = admin.IdNavigation.nombre, 
                        UserId = admin.Id, 
                        UserMail = admin.IdNavigation.email 
                    };
                else
                    return new AuthResult { Success = false, ErrorMessage = "La contraseña es incorrecta" };
            }
            
            return new AuthResult { Success = false, ErrorMessage = "No se ha encontrado una cuenta de administrador con el email proporcionado" };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO dto)
        {
            if (dto.Role != RolUsuario.Admin.ToString())
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };

            // Verificar si ya existe el email
            var emailExists = await _context.personas.AnyAsync(p => p.email == dto.Email);
            if (emailExists)
                return new AuthResult { Success = false, ErrorMessage = "El email ya está registrado" };

            // Validar que el cargo esté presente
            if (string.IsNullOrWhiteSpace(dto.Cargo))
                return new AuthResult { Success = false, ErrorMessage = "Debe ingresar el cargo del administrador" };

            // Validar la clave de Admin desde el formulario
            var adminKeyFromForm = dto.ClaveAdmin;
            
            if (string.IsNullOrEmpty(adminKeyFromForm))
                return new AuthResult { Success = false, ErrorMessage = "Debe ingresar la clave de administrador" };
            
            // Debug: verificar si existe la clave de Admin en la BD
            var adminRoleKey = await _context.role_keys.FirstOrDefaultAsync(rk => rk.Role == RolUsuario.Admin.ToString());
            if (adminRoleKey == null)
                return new AuthResult { Success = false, ErrorMessage = "Error: No existe la clave de rol Admin en la base de datos. Contacte al administrador del sistema." };
            
            if (!_hashService.VerifyPassword(adminKeyFromForm, adminRoleKey.HashedKey))
                return new AuthResult { Success = false, ErrorMessage = "La clave de administrador es incorrecta" };

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
                await _context.SaveChangesAsync();
                
                Admin admin = new Admin
                {
                    Id = persona.id,
                    Fecha_ingreso = DateOnly.FromDateTime(DateTime.UtcNow),
                    Cargo = dto.Cargo,
                    IdNavigation = persona
                };

                _context.admins.Add(admin);

                var ids= await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Admin).Select(p => p.PermisoId).ToListAsync();
                
                var personaPermisos = ids.Select(id => new PersonaPermiso
                {
                    PermisoId = id,
                    PersonaId = persona.id
                });
                
                await _context.personaPermisos.AddRangeAsync(personaPermisos);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthResult 
                { 
                    Success = true, 
                    Role = RolUsuario.Admin, 
                    UserId = persona.id, 
                    UserName = persona.nombre, 
                    UserMail = persona.email 
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new AuthResult { Success = false, ErrorMessage = "Error interno del servidor al crear el administrador" };
            }
        }
    }
}
