using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class SlotsAgenda
{
    public int id { get; set; }

    public DateOnly? fecha { get; set; }

    public TimeOnly? horainicio { get; set; }

    public TimeOnly? horafin { get; set; }

    public bool? disponible { get; set; }

    public int? medico_id { get; set; }

    public virtual Medicos? medico { get; set; }

    public virtual ICollection<Turnos> turnos { get; set; } = new List<Turnos>();
}
