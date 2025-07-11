using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class ReporteEstadistico
{
    public int id { get; set; }

    public int? consultor_id { get; set; }

    public string? tipo { get; set; }

    public DateOnly? fechadesde { get; set; }

    public DateOnly? fechahasta { get; set; }

    public virtual Persona? consultor { get; set; }
}
