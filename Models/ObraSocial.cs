using MedCenter.Attributes;

namespace MedCenter.Models;

public partial class ObraSocial
{
    public int id { get; set; }

    [NotWhiteSpace]
    public string nombre { get; set; }
    public string? sigla { get; set; }
    public bool activa { get; set; }

    public ICollection<PacienteObraSocial> pacientesObrasSociales { get; set; } = new List<PacienteObraSocial>();
    public ICollection<MedicoObraSocial> medicosObrasSociales { get; set; } = new List<MedicoObraSocial>();
}