using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Paciente
{
    public int id { get; set; }

    public string? dni { get; set; }

    public string? telefono { get; set; }

    public virtual HistoriaClinica? historiasclinicas { get; set; }

    public virtual Persona idNavigation { get; set; } = null!;

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>();
}
