using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Presentation;
using MedCenter.Attributes;

namespace MedCenter.DTOs
{
    public class RegisterDTO
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "El nombre es obligatorio")]
        [NotWhiteSpace]
        public string Nombre { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [NotWhiteSpace]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Debe seleccionar un rol")]
        [NotWhiteSpace]
        public string Role { get; set; } = string.Empty; // "Medico", "Paciente", "Secretaria"

        // Campos específicos para Paciente
        public string? DNI { get; set; }
        public string? Telefono { get; set; }

        // Campos específicos para Médico
        public string? Matricula { get; set; }
        public List<int> EspecialidadIds { get; set; } = new List<int>(); // Múltiples especialidades

        // Campos específicos para Secretaria
        public string? Legajo { get; set; }

        //Campos especificos para Admin

        public string? Cargo{get;set;}

        // Claves específicas para roles administrativos
        public string? ClaveSecretaria { get; set; } //DESPUES VER SI INCLUIR ROL ADMINISTRATIVO
        public string? ClaveMedico { get; set; }
        public string? ClaveAdmin {get;set;}
    }
}