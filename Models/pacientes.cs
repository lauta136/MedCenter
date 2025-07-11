using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Pacientes
{
    public int id { get; set; }

    public string? dni { get; set; }

    public string? telefono { get; set; }

    public virtual HistoriasClinicas? historiasclinicas { get; set; }

    public virtual Personas idNavigation { get; set; } = null!;

    public virtual ICollection<Turnos> turnos { get; set; } = new List<Turnos>();
}
