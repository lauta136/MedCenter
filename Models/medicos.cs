using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Medicos
{
    public int id { get; set; }

    public string? matricula { get; set; }

    public virtual ICollection<EntradasClinicas> entradasClinicas { get; set; } = new List<EntradasClinicas>();

    public virtual Personas idNavigation { get; set; } = null!;

    public virtual ICollection<SlotsAgenda> slotsAgenda { get; set; } = new List<SlotsAgenda>();

    public virtual ICollection<Turnos> turnos { get; set; } = new List<Turnos>();

    public virtual ICollection<MedicoEspecialidad> medicoEspecialidades { get; set; } = new List<MedicoEspecialidad>();

}
