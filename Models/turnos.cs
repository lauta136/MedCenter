using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Turnos
{
    public int id { get; set; }

    public DateOnly? fecha { get; set; }

    public TimeOnly? hora { get; set; }

    public string? estado { get; set; }

    public string? motivo_cancelacion { get; set; }

    public int? paciente_id { get; set; }

    public int? medico_id { get; set; }

    public int? secretaria_id { get; set; }

    public int? slot_id { get; set; }

    public virtual ICollection<EntradasClinicas> entradasClinicas { get; set; } = new List<EntradasClinicas>();

    public virtual Medicos? medico { get; set; }

    public virtual Pacientes? paciente { get; set; }

    public virtual Secretarias? secretaria { get; set; }

    public virtual SlotsAgenda? slot { get; set; }
}
