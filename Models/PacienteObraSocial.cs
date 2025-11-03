using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Query.Internal;
using MedCenter.Attributes;

namespace MedCenter.Models;

public partial class PacienteObraSocial
{
    public int id;
    [NotWhiteSpace]
    public int paciente_id;
    [NotWhiteSpace]
    public int obrasocial_id;

    [NotWhiteSpace(ErrorMessage = "El número de afiliado es obligatorio")]
    [StringLength(50)]
    public int numeroAfiliado;

    [StringLength(100)]
    public string? plan;
    public DateOnly fecha_afiliacion { get; set; }
    public DateOnly? fecha_baja { get; set; }
    //public bool es_principal { get; set; }
    public bool activa { get; set; } //Declarada como true por defecto en su creacion en DbContext

    public Paciente Paciente { get; set; } = null!;
    public ObraSocial obrasocial { get; set; } = null!;
    public ICollection<Turno> turnos { get; set; } = new List<Turno>();

}