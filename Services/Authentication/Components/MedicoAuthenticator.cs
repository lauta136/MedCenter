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
        private readonly RoleKeyValidationService _roleKeyValidator;

        public MedicoAuthenticator(AppDbContext appDbContext, PasswordHashService hashService, RoleKeyValidationService roleKeyValidator)
        {
            _context = appDbContext;
            _hashService = hashService;
            _roleKeyValidator = roleKeyValidator;
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
            if (dto.Role != "Medico")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };

            // Validar campos específicos de médico
            if (string.IsNullOrWhiteSpace(dto.Matricula))
                return new AuthResult { Success = false, ErrorMessage = "La matrícula es obligatoria para médicos" };

            if (dto.EspecialidadIds == null || !dto.EspecialidadIds.Any())
                return new AuthResult { Success = false, ErrorMessage = "Debe seleccionar al menos una especialidad" };

            // Validar clave de rol para médicos
            if (string.IsNullOrWhiteSpace(dto.ClaveMedico))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyValidator.GetKeyRequiredMessage("Medico") };

            if (!await _roleKeyValidator.ValidateRoleKeyAsync(dto.ClaveMedico, "Medico"))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyValidator.GetWrongKeyMessage("Medico") };

            // Verificar que las especialidades existan
            var especialidadesExistentes = await _context.especialidades
                .Where(e => dto.EspecialidadIds.Contains(e.id))
                .Select(e => e.id)
                .ToListAsync();

            if (especialidadesExistentes.Count != dto.EspecialidadIds.Count)
                return new AuthResult { Success = false, ErrorMessage = "Una o más especialidades seleccionadas no existen" };

            // Verificar si ya existe el email
            var emailExists = await _context.personas.AnyAsync(p => p.email == dto.Email);
            if (emailExists)
                return new AuthResult { Success = false, ErrorMessage = "El email ya está registrado" };

            // Verificar si ya existe la matrícula
            var matriculaExists = await _context.medicos.AnyAsync(m => m.matricula == dto.Matricula);
            if (matriculaExists)
                return new AuthResult { Success = false, ErrorMessage = "La matrícula ya está registrada" };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Crear persona
                var persona = new Persona
                {
                    nombre = dto.Nombre,
                    email = dto.Email,
                    contraseña = _hashService.HashPassword(dto.Password) // Hashear la contraseña
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
                    PersonaId = persona.id
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