using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Secretaria
{
    public int id { get; set; }

    public string? legajo { get; set; }

    public virtual Persona idNavigation { get; set; } = null!;

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>();
}
