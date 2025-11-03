using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class EntradaClinica
{
    public int id { get; set; }

    public string? diagnostico { get; set; }

    public string? tratamiento { get; set; }

    public string? observaciones { get; set; }

    public DateOnly? fecha { get; set; }

    public int? historia_id { get; set; }

    public int? turno_id { get; set; }

    public int? medico_id { get; set; }

    public virtual HistoriaClinica? historia { get; set; }

    public virtual Medico? medico { get; set; }

    public virtual Turno? turno { get; set; }
}
