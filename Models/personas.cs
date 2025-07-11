using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Personas
{
    public int id { get; set; }

    public string? nombre { get; set; }

    public string? email { get; set; }

    public string? contraseña { get; set; }

    public virtual Medicos? medicos { get; set; }

    public virtual Pacientes? pacientes { get; set; }

    public virtual ICollection<ReportesEstadisticos> reportesestadisticos { get; set; } = new List<ReportesEstadisticos>();

    public virtual Secretarias? secretarias { get; set; }
}
