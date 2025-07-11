using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class ReportesEstadisticos
{
    public int id { get; set; }

    public int? consultor_id { get; set; }

    public string? tipo { get; set; }

    public DateOnly? fechadesde { get; set; }

    public DateOnly? fechahasta { get; set; }

    public virtual Personas? consultor { get; set; }
}
