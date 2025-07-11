using MedCenter.Models;

namespace MedCenter.DTOs
{
    public class MedicoViewDTO
    {
        public string? Matricula { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public List<int> EspecialidadesIds { get; set; } = new();

        public List<int> TurnosIds { get; set; } = new();

        public MedicoViewDTO(string matricula, string nombre, string email, List<int> especialidadesIds, List<int> turnosIds)
        {
            Matricula = matricula;
            Nombre = nombre;
            Email = email;
            EspecialidadesIds = especialidadesIds;
            TurnosIds = turnosIds;
        }
        
        public MedicoViewDTO(Medico m)
        {
            Matricula = m.matricula;
            Nombre = m.idNavigation.nombre;
            Email = m.idNavigation.email;
            EspecialidadesIds = m.medicoEspecialidades.Select(me => me.especialidad.id).ToList();
            TurnosIds = m.turnos.Select(t => t.id).ToList();
        }
        
        
    }

}
