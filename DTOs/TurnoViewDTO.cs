using System.Data;
using MedCenter.Models;

namespace MedCenter.DTOs
{
   public class TurnoViewDTO
    {
        public int Id { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string? Estado { get; set; }
        public string? PacienteNombre { get; set; }
        public string? MedicoNombre { get; set; }
        public string? Especialidad { get; set; }

        public TurnoViewDTO() { }
    }
}
