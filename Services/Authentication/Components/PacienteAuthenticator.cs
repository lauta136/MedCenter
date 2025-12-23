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

        public PacienteAuthenticator(AppDbContext appDbContext, PasswordHashService passwordHashService)
        {
            _context = appDbContext;
            _hashService = passwordHashService;
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {

            var paciente = await _context.pacientes
            .Include(p => p.idNavigation)
            .Where(p => p.idNavigation.email == username)
            .FirstOrDefaultAsync();

            if (paciente != null)
            {
                if (_hashService.VerifyPassword(password, paciente.idNavigation.contraseña))
                    return new AuthResult { Success = true, Role = RolUsuario.Paciente, UserName = paciente.idNavigation.nombre, UserId = paciente.idNavigation.id, UserMail = paciente.idNavigation.email };
                else
                    return new AuthResult { Success = false, ErrorMessage = "La contraseña es incorrecta" };
            }
            else
                return new AuthResult { Success = false, ErrorMessage = "No se ha encontrado una cuenta de paciente con el mail proporcionado" };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO registerDto)
        {
            if (registerDto.Role != "Paciente")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };

            if (string.IsNullOrEmpty(registerDto.DNI))
                return new AuthResult { Success = false, ErrorMessage = "El campo dni esta vacio" };

            if (registerDto.DNI.Length < 7)
                return new AuthResult { Success = false, ErrorMessage = "El campo dni no contiene el largo adecuado" };

            var pac = await _context.pacientes.FirstOrDefaultAsync(p => p.dni == registerDto.DNI);
            if (pac != null)
                return new AuthResult { Success = false, ErrorMessage = "Este dni ya corresponde a un paciente" };

            if (string.IsNullOrEmpty(registerDto.Telefono))
                return new AuthResult { Success = false, ErrorMessage = "El campo telefono esta vacio" };

            if (registerDto.Telefono.Length < 10)
                return new AuthResult { Success = false, ErrorMessage = "El campo telefono no contiene el largo adecuado" };

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
                    PersonaId = persona.id
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