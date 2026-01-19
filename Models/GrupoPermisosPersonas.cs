namespace MedCenter.Models;

public class GrupoPermisosPersonas
{
    public int Id {get;set;}

    public string Nombre {get;set;} 
    public string? Descripcion {get;set;} 
    public DateOnly FechaCreacion {get;set;} 

    public virtual List<PersonaGrupo> Personas{get;set;}
    public virtual List<PermisoGrupo> Permisos{get;set;}

}