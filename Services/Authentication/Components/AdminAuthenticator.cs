using MedCenter.Data;
using MedCenter.DTOs;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.TurnoSv;
using MedCenter.Models;

namespace MedCenter.Services.Authentication.Components
{
    public class AdminAuthenticator : IAuthComponent
    {
        private readonly AppDbContext _context;
        private readonly PasswordHashService _hashService;
        private RoleKeyValidationService _roleKeyService;


        public AdminAuthenticator(AppDbContext appDbContext, PasswordHashService passwordHashService, RoleKeyValidationService roleKeyValidationService)
        {
            _context = appDbContext;
            _hashService = passwordHashService;
            _roleKeyService = roleKeyValidationService;

        }
        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            var admin = await _context.admins.Include(a => a.IdNavigation).FirstOrDefaultAsync(a => a.IdNavigation.email == username);

            if(admin == null)
            return new AuthResult{Success = false, ErrorMessage = "No se ha encontrado una cuenta de admin con el mail proporcionado"};
            
            if(!_hashService.VerifyPassword(password, admin.IdNavigation.contraseña))
            return new AuthResult{Success = false, ErrorMessage = "La contraseña es incorrecta"};

            return new AuthResult { Success = true, Role = RolUsuario.Admin, UserName = admin.IdNavigation.nombre, UserId = admin.IdNavigation.id, UserMail = admin.IdNavigation.email };


        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO dto)
        {
            if (dto.Role != "Admin")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };
            // Verificar si ya existe el email
            var emailExists = await _context.personas.AnyAsync(p => p.email == dto.Email);
            if (emailExists)
                return new AuthResult { Success = false, ErrorMessage = "El email ya está registrado" };

            if (string.IsNullOrEmpty(dto.ClaveAdmin))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetKeyRequiredMessage("Admin") };
            if (!await _roleKeyService.ValidateRoleKeyAsync(dto.ClaveAdmin, "Admin"))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetWrongKeyMessage("Admin") };

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

                Admin admin = new Admin
                {
                    Id = persona.id,
                    Cargo = dto.Cargo,
                    Activo = true,
                    IdNavigation = persona
                };

                _context.admins.Add(admin);


                var ids= await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Admin).Select(p => p.PermisoId).ToListAsync();
                
                
                var personaPermisos = ids.Select(id => new PersonaPermiso
                {
                    PermisoId = id,
                    PersonaId = persona.id,
                    Origen = PermisoSource.Role
                });
                
                await _context.personaPermisos.AddRangeAsync(personaPermisos);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthResult { Success = true, Role = RolUsuario.Admin, UserId = persona.id, UserName = persona.nombre, UserMail = persona.email };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new AuthResult { Success = false, ErrorMessage = "Error interno del servidor al crear el admin" };
            }
            
        }
        }
    }
