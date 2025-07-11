using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class HistoriasClinicas
{
    public int id { get; set; }

    public int? paciente_id { get; set; }

    public virtual ICollection<EntradasClinicas> EntradasClinicas { get; set; } = new List<EntradasClinicas>();

    public virtual Pacientes? paciente { get; set; }
}
