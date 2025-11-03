
namespace MedCenter.DTOs
{
    using MedCenter.Models;
    public class PacienteViewDTO
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Dni { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }

        public PacienteViewDTO() { }

        // Constructor útil para mapear desde el modelo
        public PacienteViewDTO(Paciente paciente)
        {
            Id = paciente.id;
            Nombre = paciente.idNavigation?.nombre;
            Dni = paciente.dni;
            Telefono = paciente.telefono;
            Email = paciente.idNavigation?.email;
        }
    }
}