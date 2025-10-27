using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MedCenter.Models;

public partial class SlotAgenda //Adentro de cada bloque (DisponibilidadMedico) se encuentran los slots de la agenda
{
    public int id { get; set; }

    public DateOnly? fecha { get; set; } //no deberia ser nullable

    public TimeOnly? horainicio { get; set; }

    public TimeOnly? horafin { get; set; }

    public bool? disponible { get; set; }

    public int? medico_id { get; set; }

    public virtual Medico? medico { get; set; }

    public int bloqueDisponibilidadId { get; set; }

    [Required]
    public DisponibilidadMedico bloqueDisponibilidad { get; set; }

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>(); //creo deberia cambiar a un solo turno?, no coleccion
} 
