using MedCenter.Services.TurnoSv;
using MedCenter.Enums;
using System.ComponentModel.DataAnnotations;

namespace MedCenter.DTOs;

public class UserPermissionDTO
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    public RolUsuario Role { get; set; }
    public bool IsActive { get; set; }
}

public class PermissionDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public Recurso Recurso { get; set; }
    public AccionUsuario Accion { get; set; }
}

public class PermissionGroupDTO
{
    public string GroupKey { get; set; }
    public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    public List<UserPermissionDTO> Users { get; set; } = new List<UserPermissionDTO>();
}


public class AssignRolePermissionsDTO
{
    public int UserId { get; set; }
    public RolUsuario Role { get; set; }
}


public class AssignPermissionToGroupDTO
{
    public List<int> UsersIds { get; set; } = new List<int>();
    public int PermissionId { get; set; }
}

public class RemovePermissionFromGroupDTO
{
    public List<int> UsersIds { get; set; } = new List<int>();
    public int PermissionId { get; set; }
}

public class CreateGroupDTO
{
    [MinLength(1,ErrorMessage ="Debe seleccionar al menos un usuario")]
    public int [] UserIds {get;set;} = Array.Empty<int>();

    [MinLength(1,ErrorMessage ="Debe seleccionar al menos un permiso")]
    public int [] PermissionIds {get;set;}= Array.Empty<int>();

    [Required]
    public string Name {get;set;}

    public string? Description{get;set;}
}


public class PermissionManagementViewModel
{
    public List<UserPermissionDTO> AllUsers { get; set; } = new List<UserPermissionDTO>();
    public List<PermissionDTO> AllPermissions { get; set; } = new List<PermissionDTO>();
    public List<PermissionGroupDTO> PermissionGroups { get; set; } = new List<PermissionGroupDTO>();
    public Dictionary<RolUsuario, List<PermissionDTO>> RolePermissions { get; set; } = new Dictionary<RolUsuario, List<PermissionDTO>>();
}

public class RemovePermissionFromUserDTO
{
    public int UserId{get;set;}
    public int PermissionId{get;set;}
}
public class AssingPermissionToUserDTO
{
    public int UserId{get;set;}
    public int PermissionId{get;set;}
}

public class DeactivateAccount
{
    public int UserId{get;set;}
    public string Role{get;set;}
    public bool Force{get;set;}
}
public class ActivateAccount
{
    public int UserId{get;set;}
    public string Role{get;set;}
}

public class UpdateRoleKeyDTO
{
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "La clave debe tener al menos 6 caracteres.")]
    public string NewKey { get; set; } = string.Empty;
}