using System.Data;
using MedCenter.Models;

namespace MedCenter.DTOs
{
    public class MedicoViewDTO
    {
        public int Id { get; set; }
        public string? Matricula { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public List<string>? Especialidades { get; set; } = new();

        public List<int>? TurnosIds { get; set; } = new();

        public MedicoViewDTO() { } //Creado para usar en controlador a la hora de hacer VIEWS, para poder usar SELECTS de SQL y no usar INCLUDES que traen mas informacion de la necesaria

        //para Hacer por ejemplo UPDATES ahi si usar Include.
        public MedicoViewDTO(int id, string matricula, string nombre, string email, List<string> especialidades, List<int> turnosIds)
        {
            Id = id;
            Matricula = matricula;
            Nombre = nombre;
            Email = email;
            Especialidades = especialidades;
            //TurnosIds = turnosIds;
        }

        public MedicoViewDTO(Medico m)
        {
            Id = m.id;
            Matricula = m.matricula;
            Nombre = m.idNavigation.nombre;
            Email = m.idNavigation.email;
            Especialidades = m.medicoEspecialidades
                .Select(me => me.especialidad.nombre ?? "Sin nombre")
                .ToList();
            //TurnosIds = m.turnos.Select(t => t.id).ToList();
        }
        
        
    }

}
