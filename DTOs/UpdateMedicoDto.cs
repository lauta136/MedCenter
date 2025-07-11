namespace MedCenter.DTOs
{
    public class UpdateMedicoDto
    {
        public string? Matricula { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public List<int> EspecialidadesIds { get; set; } = new();
    }
}
