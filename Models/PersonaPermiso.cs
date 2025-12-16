namespace MedCenter.Models;

public class PersonaPermiso
{
    public int PermisoId{get;set;}
    public int PersonaId{get;set;}
    public virtual Persona Persona {get;set;}
    public virtual Permiso Permiso {get;set;}

}