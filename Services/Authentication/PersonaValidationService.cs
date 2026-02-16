using MedCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.Authentication;

public class PersonaValidationService
{   
    private readonly AppDbContext _context;
    private RoleKeyValidationService _roleKeyService;

    public PersonaValidationService(AppDbContext context, RoleKeyValidationService roleKeyValidationService)
    {
        _context = context;
        _roleKeyService = roleKeyValidationService;
    }
    public async Task<AuthResult> ValidatePacienteAsync(string mail, string rol, string dni, string telefono)
    {
        if (rol != "Paciente")
            return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };

        if (string.IsNullOrEmpty(dni))
            return new AuthResult { Success = false, ErrorMessage = "El campo dni esta vacio" };

        if (dni.Length < 7)
            return new AuthResult { Success = false, ErrorMessage = "El campo dni no contiene el largo adecuado" };
            if (string.IsNullOrEmpty(telefono))
            return new AuthResult { Success = false, ErrorMessage = "El campo telefono esta vacio" };

        if (telefono.Length < 10)
            return new AuthResult { Success = false, ErrorMessage = "El campo telefono no contiene el largo adecuado" };

        if (await _context.pacientes.AnyAsync(p => p.dni == dni))
            return new AuthResult { Success = false, ErrorMessage = "Este dni ya corresponde a un paciente" };
        
        var result = await ValidateEmail(mail);
        if(!result.Success)
        return result;

        return new AuthResult{Success = true};
    }

    public async Task<AuthResult> ValidateSecretariaAsync(string mail, string rol, string ClaveSecretaria, string legajo) //Fijate comom validar el elegajo
    {
        if (rol != "Secretaria")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };
        if(string.IsNullOrWhiteSpace(legajo))
            return new AuthResult{Success = false, ErrorMessage = "Campo de legajo vacio"};
        if (string.IsNullOrEmpty(ClaveSecretaria))
            return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetKeyRequiredMessage("Secretaria") };
        if (!await _roleKeyService.ValidateRoleKeyAsync(ClaveSecretaria, "Secretaria"))
            return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetWrongKeyMessage("Secretaria") };

        var result = await ValidateEmail(mail);
        if(!result.Success)
        return result;

        return new AuthResult{Success = true};
    }

    public async Task<AuthResult> ValidateMedicoAsync(string mail, string rol, string matricula, List<int> especialidadesIds, string ClaveMedico)
    {
        if (rol!= "Medico")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };

            // Validar campos específicos de médico
            if (string.IsNullOrWhiteSpace(matricula))
                return new AuthResult { Success = false, ErrorMessage = "La matrícula es obligatoria para médicos" };

            if (especialidadesIds == null || !especialidadesIds.Any())
                return new AuthResult { Success = false, ErrorMessage = "Debe seleccionar al menos una especialidad" };

            // Validar clave de rol para médicos
            if (string.IsNullOrWhiteSpace( ClaveMedico))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetKeyRequiredMessage("Medico") };

            if (!await _roleKeyService.ValidateRoleKeyAsync(ClaveMedico, "Medico"))
                return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetWrongKeyMessage("Medico") };

            // Verificar que las especialidades existan
            var especialidadesExistentes = await _context.especialidades
                .Where(e => especialidadesIds.Contains(e.id))
                .Select(e => e.id)
                .ToListAsync();

            if (especialidadesExistentes.Count != especialidadesIds.Count)
                return new AuthResult { Success = false, ErrorMessage = "Una o más especialidades seleccionadas no existen" };

            var result = await ValidateEmail(mail);
            if(!result.Success)
            return result;
            // Verificar si ya existe la matrícula
            var matriculaExists = await _context.medicos.AnyAsync(m => m.matricula == matricula);
            if (matriculaExists)
                return new AuthResult { Success = false, ErrorMessage = "La matrícula ya está registrada" };

        return new AuthResult{Success = true};
    }
    public async Task<AuthResult> ValidateEmail(string mail)
    {
        if(await _context.personas.AnyAsync(p => p.email == mail))
            return new AuthResult{Success = false, ErrorMessage = "Este email ya corresponde a otra cuenta"};
        
        return new AuthResult {Success = true};
    }

    public async Task<AuthResult> ValidateAdminAsync(string rol, string ClaveAdmin,string mail)
    {
         if (rol != "Admin")
                return new AuthResult { Success = false, ErrorMessage = "Rol incorrecto para este autenticador" };
            // Verificar si ya existe el email
        if (string.IsNullOrWhiteSpace(ClaveAdmin))
            return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetKeyRequiredMessage("Admin") };
        var result = await ValidateEmail(mail);
        if(!result.Success)
        return result;
        if (!await _roleKeyService.ValidateRoleKeyAsync(ClaveAdmin, "Admin"))
            return new AuthResult { Success = false, ErrorMessage = _roleKeyService.GetWrongKeyMessage("Admin") };
        return new AuthResult {Success = true};
    }
}