using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedCenter.Model;

namespace MedCenter.Models;

public partial class Persona
{
    [Key]
    public int id { get; set; }

    public string? nombre { get; set; }

    public string? email { get; set; }

    public string? contraseña { get; set; }

    public virtual Medico? Medico { get; set; }

    public virtual Paciente? Paciente { get; set; }

    public virtual Admin? Admin {get;set;}

    public virtual ICollection<ReporteEstadistico> reportesestadisticos { get; set; } = new List<ReporteEstadistico>();
    
    public virtual ICollection<PersonaPermiso> PersonaPermisos{get;set;}
    public virtual Secretaria? Secretaria { get; set; }

}
