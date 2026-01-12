using MedCenter.Services.TurnoSv;
using MedCenter.Enums;

namespace MedCenter.DTOs;

public class UserPermissionDTO
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public List<PermissionDTO> Permissions { get; set; } = new List<PermissionDTO>();
    public List<RolUsuario> Roles { get; set; } = new List<RolUsuario>();
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

public class AssignPermissionDTO
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
}

public class AssignRolePermissionsDTO
{
    public int UserId { get; set; }
    public RolUsuario Role { get; set; }
}

public class RemovePermissionDTO
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
}

public class PermissionManagementViewModel
{
    public List<UserPermissionDTO> AllUsers { get; set; } = new List<UserPermissionDTO>();
    public List<PermissionDTO> AllPermissions { get; set; } = new List<PermissionDTO>();
    public List<PermissionGroupDTO> PermissionGroups { get; set; } = new List<PermissionGroupDTO>();
    public Dictionary<RolUsuario, List<PermissionDTO>> RolePermissions { get; set; } = new Dictionary<RolUsuario, List<PermissionDTO>>();
}
