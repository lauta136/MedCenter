using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Persona
{
    public int id { get; set; }

    public string? nombre { get; set; }

    public string? email { get; set; }

    public string? contraseña { get; set; }

    public virtual Medico? medicos { get; set; }

    public virtual Paciente? pacientes { get; set; }

    public virtual ICollection<ReporteEstadistico> reportesestadisticos { get; set; } = new List<ReporteEstadistico>();

    public virtual Secretaria? secretarias { get; set; }
}
