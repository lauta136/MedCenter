using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Secretarias
{
    public int id { get; set; }

    public string? legajo { get; set; }

    public virtual Personas idNavigation { get; set; } = null!;

    public virtual ICollection<Turnos> turnos { get; set; } = new List<Turnos>();
}
