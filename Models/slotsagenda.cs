using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class SlotAgenda
{
    public int id { get; set; }

    public DateOnly? fecha { get; set; }

    public TimeOnly? horainicio { get; set; }

    public TimeOnly? horafin { get; set; }

    public bool? disponible { get; set; }

    public int? medico_id { get; set; }

    public virtual Medico? medico { get; set; }

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>();
}
