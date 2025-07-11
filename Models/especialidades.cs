using System;
using System.Collections.Generic;

namespace MedCenter.Models;

public partial class Especialidad
{
    public int id { get; set; }

    public string? nombre { get; set; }
    public virtual ICollection<MedicoEspecialidad> medicoEspecialidades { get; set; } = new List<MedicoEspecialidad>();

}
