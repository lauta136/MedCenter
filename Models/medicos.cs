using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Medico
{
    public int id { get; set; }

    public string? matricula { get; set; }

    public virtual ICollection<EntradaClinica> entradasClinicas { get; set; } = new List<EntradaClinica>();

    public virtual Persona idNavigation { get; set; } = null!;

    public virtual ICollection<SlotAgenda> slotsAgenda { get; set; } = new List<SlotAgenda>();

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>();

    public virtual ICollection<MedicoEspecialidad> medicoEspecialidades { get; set; } = new List<MedicoEspecialidad>();

}
