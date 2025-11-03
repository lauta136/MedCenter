using System.Data;
using MedCenter.Models;

namespace MedCenter.DTOs
{
    public class MedicoAgendaDTO
    {
        public int Id { get; set; }
        public string? Matricula { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public List<int>? TurnosIds { get; set; } = new();
        public ICollection<SlotAgenda> SlotsAgenda = new List<SlotAgenda>();
        public ICollection<DisponibilidadMedico> DiasDisponibles = new List<DisponibilidadMedico>();


        //para Hacer por ejemplo UPDATES ahi si usar Include.
        public MedicoAgendaDTO(int id,  string nombre,string matricula,ICollection<SlotAgenda> slotAgendas, ICollection<DisponibilidadMedico> disponibilidadMedicos)
        {
            Id = id;
            Matricula = matricula;
            Nombre = nombre;
            SlotsAgenda = slotAgendas;
            DiasDisponibles = disponibilidadMedicos;
            //Especialidades = especialidades;
            //TurnosIds = turnosIds;
        }


        /*public MedicoAgendaDTO(Medico m)
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
        */
        
    }

}
