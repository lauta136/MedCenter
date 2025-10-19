// En una nueva carpeta DTOs/ o en el mismo archivo si prefieres
using System.ComponentModel.DataAnnotations;
using System.Data;
using MedCenter.Models;
using MedCenter.Attributes;

namespace MedCenter.DTOs // <-- Asegurate de que esta línea sea correcta
{
    public class TurnoCancelDTO
    {
        public int Id { get; set; }

        // Propiedades para mostrar en la vista de confirmación (son de solo lectura)
        public string? Fecha { get; set; }
        public string? Hora { get; set; }
        public string? PacienteNombre { get; set; }
        public string? MedicoNombre { get; set; }
        public string? Estado { get; set; }


        // Propiedad para el formulario
        //[Required(ErrorMessage = "El motivo de cancelación es obligatorio.")]
        [NotWhiteSpace]
        public string? MotivoCancelacion { get; set; }
    }
}