using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Turno
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

    public virtual ICollection<EntradaClinica> entradasClinicas { get; set; } = new List<EntradaClinica>();

    public virtual Medico? medico { get; set; }

    public virtual Paciente? paciente { get; set; }

    public virtual Secretaria? secretaria { get; set; }

    public virtual SlotAgenda? slot { get; set; }
}
