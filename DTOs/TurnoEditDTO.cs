using System.ComponentModel.DataAnnotations;
using MedCenter.Services.TurnoSv;

namespace MedCenter.DTOs
{
    public class TurnoEditDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateOnly? Fecha { get; set; }

        [Required(ErrorMessage = "La hora es obligatoria.")]
        public TimeOnly? Hora { get; set; }

        public int? PacienteId { get; set; } // Ya no es requerido desde el formulario
        public string? PacienteNombre { get; set; } // Para mostrar el nombre

        [Required(ErrorMessage = "Debe seleccionar un médico.")]
        public int? MedicoId { get; set; }

        public int EspecialidadId { get; set; }
        public string? EspecialidadNombre { get; set; }

        public EstadosTurno? Estado {get;set;}
        public string? DescripcionEstado{get;set;}

    }
}
