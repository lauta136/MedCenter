using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Linq;

namespace MedCenter.Services.Authentication.Components
{
    public class PacienteAuthenticator : IAuthComponent
    {
        private AppDbContext _context;
        private PasswordHashService _hashService;
        private readonly PersonaValidationService _validationService;

        public PacienteAuthenticator(AppDbContext appDbContext, PasswordHashService passwordHashService, PersonaValidationService personaValidationService)
        {
            _context = appDbContext;
            _hashService = passwordHashService;
            _validationService = personaValidationService;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {

            var paciente = await _context.pacientes
            .Include(p => p.idNavigation)
            .Where(p => p.idNavigation.email == username)
            .FirstOrDefaultAsync();

            if (paciente == null)
                return new AuthResult { Success = false, ErrorMessage = "No se ha encontrado una cuenta de paciente con el mail proporcionado" };
            if(paciente.idNavigation.activo == false)
            return new AuthResult{Success = false, ErrorMessage = "La cuenta ha sido desactivada"};

            if (_hashService.VerifyPassword(password, paciente.idNavigation.contraseña))
                return new AuthResult { Success = true, Role = RolUsuario.Paciente, UserName = paciente.idNavigation.nombre, UserId = paciente.idNavigation.id, UserMail = paciente.idNavigation.email };
            else
                return new AuthResult { Success = false, ErrorMessage = "La contraseña es incorrecta" };
            

        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO registerDto)
        {
            
            var result = await _validationService.ValidatePacienteAsync(registerDto.Email, registerDto.Role,registerDto.DNI,registerDto.Telefono);
            if(result.Success == false)
            return result;

            var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Persona persona = new Persona
                {
                    nombre = registerDto.Nombre,
                    email = registerDto.Email,
                    contraseña = _hashService.HashPassword(registerDto.Password)
                };

                await _context.personas.AddAsync(persona);
                await _context.SaveChangesAsync();

                Paciente paciente = new Paciente
                {
                    id = persona.id,
                    dni = registerDto.DNI,
                    telefono = registerDto.Telefono,
                    idNavigation = persona
                };

                await _context.pacientes.AddAsync(paciente);
                var ids= await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Paciente).Select(p => p.PermisoId).ToListAsync();
                
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
                    UserId = paciente.id,
                    UserName = persona.nombre,
                    UserMail = persona.email,
                    Role = RolUsuario.Paciente
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += " | Inner Exception: " + ex.InnerException.Message;

                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Error al crear el paciente: {errorMessage}"
                };
            }
        }

    }
}