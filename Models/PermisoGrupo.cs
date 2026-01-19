namespace MedCenter.Models;

public class PermisoGrupo
{
    public int PermisoId {get;set;}
    public int GrupoId {get;set;}

    public virtual Permiso Permiso {get;set;}
    public virtual GrupoPermisosPersonas Grupo {get;set;}

}