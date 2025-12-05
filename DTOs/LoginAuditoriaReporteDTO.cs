using MedCenter.Models;

namespace MedCenter.DTOs;

public class LoginAuditoriaReporteDTO
{
    public int Id { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioRol { get; set; } = string.Empty;
    public DateTime MomentoLogin { get; set; }
    public DateTime? MomentoLogout { get; set; }
    public string? TipoLogout { get; set; }
    public TimeSpan? DuracionSesion { get; set; }
}
