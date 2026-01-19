namespace MedCenter.Models;

public class PersonaGrupo
{
    public int PersonaId {get;set;}
    public int GrupoId {get;set;}

    public virtual Persona Persona{get;set;}
    public virtual GrupoPermisosPersonas Grupo{get;set;}

}