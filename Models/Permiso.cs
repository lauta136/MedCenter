using MedCenter.Enums;

namespace MedCenter.Models;

public class Permiso
{
    public int Id{get;set;}
    public string Nombre{get;set;}
    public string Descripcion{get;set;}
    public Recurso Recurso { get; set; } // e.g., "User"
    public AccionUsuario Accion { get; set; } // e.g., "Create"
    
    public Permiso? PermisoPadre{get;set;}
    public int? PermisoPadreId {get;set;}
    public ICollection<Permiso> PermisosHijos{get;set;} = new List<Permiso>();
    public ICollection<PersonaPermiso> PersonaPermisos { get; set; }
    public ICollection<RolPermiso> RolPermisos { get; set; }
}
    
