using DocumentFormat.OpenXml.Office2010.Excel;
using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;
using System.Linq;
namespace MedCenter.Services.Authentication.Components
{
    public class MedicoAuthenticator : IAuthComponent
    {
        private readonly AppDbContext _context;
        private readonly PasswordHashService _hashService;
        private readonly PersonaValidationService _validationService;

        public MedicoAuthenticator(AppDbContext appDbContext, PasswordHashService hashService, PersonaValidationService personaValidationService)
        {
            _context = appDbContext;
            _hashService = hashService;
            _validationService = personaValidationService;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            // Primero busca la persona
            var persona = await _context.personas
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.email == username);

            if (persona == null)
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "No se ha encontrado una cuenta con el mail proporcionado"
                };

            // Verifica si es médico
            if (persona.Medico == null)
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "El usuario no es un médico"
                };

            

            if (!_hashService.VerifyPassword(password, persona.contraseña))
                return new AuthResult { Success = false, ErrorMessage = "La contraseña es incorrecta" };

            return new AuthResult
            {
                Success = true,
                Role = RolUsuario.Medico,
                UserName = persona.nombre,
                UserId = persona.id,
                UserMail = persona.email
            };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO dto)
        {
            var result = await _validationService.ValidateMedicoAsync(dto.Email,dto.Role,dto.Matricula,dto.EspecialidadIds,dto.ClaveMedico,null);
            if (!result.Success)
                return result;
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Crear persona
                var persona = new Persona
                {
                    nombre = dto.Nombre,
                    email = dto.Email,
                    contraseña = _hashService.HashPassword(dto.Password), // Hashear la contraseña
                    activo = true
                };

                _context.personas.Add(persona);
                await _context.SaveChangesAsync();

                // Crear médico
                var medico = new Medico
                {
                    id = persona.id,
                    matricula = dto.Matricula,
                    idNavigation = persona
                };

                _context.medicos.Add(medico);
                //await _context.SaveChangesAsync();

                // Crear relaciones con especialidades
                foreach (var especialidadId in dto.EspecialidadIds)
                {
                    _context.medicoEspecialidades.Add(new MedicoEspecialidad
                    {
                        medicoId = medico.id,
                        especialidadId = especialidadId
                    });
                }


                var ids= await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Medico).Select(p => p.PermisoId).ToListAsync();
                
                var personaPermisos = ids.Select(id => new PersonaPermiso
                {
                    PermisoId = id,
                    PersonaId = persona.id,
                    Origen = PermisoSource.Role
                });
                
                await _context.personaPermisos.AddRangeAsync(personaPermisos);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthResult
                {
                    Success = true,
                    Role = RolUsuario.Medico,
                    UserId = medico.id,
                    UserName = persona.nombre,
                    UserMail = persona.email
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return new AuthResult { Success = false, ErrorMessage = "Error interno del servidor al crear el médico" };
            }
        }
    }
}