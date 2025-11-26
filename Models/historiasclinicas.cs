using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class HistoriaClinica
{
    public int id { get; set; }

    public int paciente_id { get; set; }

    public virtual ICollection<EntradaClinica> EntradasClinicas { get; set; } = new List<EntradaClinica>();

    public virtual Paciente paciente { get; set; }
}
