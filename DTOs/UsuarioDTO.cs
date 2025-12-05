namespace MedCenter.DTOs;

public class UsuarioDTO
{
    public int Id{get;set;}
    public required string Nombre {get;set;}
    public List<string> Acciones {get;set;} = new List<string>();
}