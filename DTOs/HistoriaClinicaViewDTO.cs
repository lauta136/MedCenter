namespace MedCenter.DTOs;

public class HistoriaClinicaViewDTO
{
    public int? HistoriaId { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; }
    public string PacienteDni { get; set; }
    public string PacienteEmail { get; set; }
    public string PacienteTelefono { get; set; }
    public List<EntradaClinicaViewDTO> Entradas { get; set; } = new List<EntradaClinicaViewDTO>();
}

public class EntradaClinicaViewDTO
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string MedicoNombre { get; set; }
    public string Diagnostico { get; set; }
    public string Tratamiento { get; set; }
    public string? Observaciones { get; set; }
}
