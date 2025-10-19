using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace MedCenter.Models;

public partial class Turno
{

    public int id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una fecha.")]
    public DateOnly? fecha { get; set; }

    [Required(ErrorMessage = "Debe seleccionar una hora.")]
    public TimeOnly? hora { get; set; }

    public string? estado { get; set; }

    public string? motivo_cancelacion { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un paciente.")]
    public int? paciente_id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un médico.")]
    public int? medico_id { get; set; }

    public int? especialidad_id { get; set; }

    public int? secretaria_id { get; set; }

    public int? slot_id { get; set; }

    public int? pacienteobrasocial_id { get; set; }

    public bool es_particular { get; set; }
      
    public virtual ICollection<EntradaClinica> entradasClinicas { get; set; } = new List<EntradaClinica>();

    public virtual Medico? medico { get; set; }

    public virtual Paciente? paciente { get; set; }

    public virtual Especialidad? especialidad { get; set; }

    public virtual Secretaria? secretaria { get; set; }

    public virtual SlotAgenda? slot { get; set; }

    public PacienteObraSocial ? paciente_obrasocial { get; set; } // NUEVO

}
