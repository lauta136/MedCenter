using MedCenter.Services.TurnoSv;

namespace MedCenter.Models;

public class TrazabilidadLogin
{
    public int Id {get;set;}
    public required DateTime MomentoLogin{get;set;}
    public required string UsuarioNombre {get;set;}
    public required int UsuarioId {get;set;}
    public required RolUsuario UsuarioRol{get;set;}
    public DateTime? MomentoLogout {get;set;}
    public TipoLogout? TipoLogout {get;set;}
}

public enum TipoLogout
{
    Manual,
    Timeout,
    Forzado
}