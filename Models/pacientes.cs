using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedCenter.Models;

public partial class Paciente
{
    [Key]
    [ForeignKey(nameof(idNavigation))]
    public int id { get; set; }

    public string? dni { get; set; }

    public string? telefono { get; set; }

    public virtual HistoriaClinica? historiasclinicas { get; set; }

    public virtual Persona idNavigation { get; set; } = null!;

    public virtual ICollection<Turno> turnos { get; set; } = new List<Turno>();

    public ICollection<PacienteObraSocial> pacientesObrasSociales { get; set; } = new List<PacienteObraSocial>();
}
