using MedCenter.DTOs;
using MedCenter.Services.AdminService;
using MedCenter.Services.TurnoSv;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MedCenter.Attributes;
using MedCenter.Extensions;
using MedCenter.Models;
namespace MedCenter.Controllers;

[Route("[controller]")]
public class AdminController : BaseController
{
    private readonly AdminService _adminService;
    private readonly TurnoService _turnoService;
    private readonly AuthService _authService;

    public AdminController(AdminService adminService, TurnoService turnoService, AuthService authService)
    {
        _adminService = adminService;
        _turnoService = turnoService;
        _authService = authService;
    }
    
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.EsAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        return View();
    }

    // View for permission management panel
    [HttpGet("PanelAdmin")]
    public async Task<IActionResult> PanelAdmin()
    {
        var isAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        if (!isAdmin)
        {
            return RedirectToAction("AccessDenied", "Access");
        }
        ViewBag.EsAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        ViewBag.Rol = UserRole;
        ViewBag.CanViewAllPermissions = await _adminService.VerPermisos(UserId.Value);
        ViewBag.CanViewGroups = await _adminService.VerGrupos(UserId.Value);
        ViewBag.CanViewRoles = await _adminService.VerRoles(UserId.Value);
        var viewModel = await _adminService.GetPermissionManagementData();
        await _turnoService.FinalizarAusentarTurnosPasados();
        return View(viewModel);
    }

    // API endpoint to get all users with permissions
    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersWithPermissions();
        return Ok(users);
    }

    // API endpoint to get all permissions
    [HttpGet("GetAllPermissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _adminService.GetAllPermissions();
        return Ok(permissions);
    }

    // API endpoint to assign permission to user
    [RequiredPermission("permiso:assign")]
    [HttpPost("AssignPermission")]
    public async Task<IActionResult> AssignPermission([FromBody] AssingPermissionToUserDTO dto)
    {
        var result = await _adminService.AssignPermissionToUser(dto.UserId, dto.PermissionId);
        if (result.Success)
        {
            return BadRequest(new { success = true, message = "Permiso asignado correctamente" });
        }
        return Ok(new { success = false, message = "Error al asignar el permiso" });
    }

    // API endpoint to remove permission from user
    [RequiredPermission("permiso:remove")]
    [HttpPost("RemovePermission")]
    public async Task<IActionResult> RemovePermission([FromBody] RemovePermissionFromUserDTO dto)
    {
        var result = await _adminService.RemovePermissionFromUser(dto.UserId, dto.PermissionId);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Permiso removido correctamente" });
        }
        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // API endpoint to deactivate an account
    [RequiredPermission("persona:deactivate")]
    [HttpPost("DeactivateAccpunt")]
    public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccount dto)
    {
        string requiredPerm = dto.Role.ToRolUsuario() switch
        {
            RolUsuario.Paciente => "paciente:delete",
            RolUsuario.Secretaria => "secretaria:delete",
            RolUsuario.Medico => "medico:delete",
            RolUsuario.Admin => "admin:delete",
            _ => "persona:edit"
        };
        
        if(!await _adminService.TienePermiso(UserId.Value, requiredPerm))
            return StatusCode(403, new { success = false, message = "No tienes permiso para desactivar este usuario." });

        var result = await _adminService.DesactivarCuenta(dto.UserId, dto.Role);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Cuenta desactivada correctamente" });
        }
        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // API endpoint to deactivate an account
    [RequiredPermission("persona:activate")]
    [HttpPost("ActivateAccount")]
    public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccount dto)
    {
        string requiredPerm = dto.Role.ToRolUsuario() switch
        {
            RolUsuario.Paciente => "paciente:activate",
            RolUsuario.Secretaria => "secretaria:activate",
            RolUsuario.Medico => "medico:activate",
            RolUsuario.Admin => "admin:activate",
            _ => "persona:activate"
        };
        
        if(!await _adminService.TienePermiso(UserId.Value, requiredPerm))
            return StatusCode(403, new { success = false, message = "No tienes permiso para activar este usuario." });

        var result = await _adminService.ReactivarCuenta(dto.UserId, dto.Role);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Cuenta activada correctamente" });
        }
        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    [HttpGet("EditAccount")]
    public async Task<IActionResult> EditAccountGet([FromQuery] int userId, [FromQuery] string rol)
    {
        string requiredPerm = rol.ToRolUsuario() switch
        {
            RolUsuario.Paciente => "paciente:edit",
            RolUsuario.Secretaria => "secretaria:edit",
            RolUsuario.Medico => "medico:edit",
            RolUsuario.Admin => "admin:edit",
            _ => "persona:edit"
        };
        
        if(!await _adminService.TienePermiso(UserId.Value, requiredPerm))
            return StatusCode(403, new { success = false, message = "No tienes permiso para editar este usuario." });

        try
        {
            var result = await _adminService.GetEditar(rol, userId);
            if (result != null)
                return Ok(new { success = true, data = result });

            return BadRequest(new { success = false, message = "No se encontraron los datos del usuario." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    [HttpPost("EditAccount")]
    public async Task<IActionResult> EditAccount([FromBody] PersonaEditDTO dto, [FromQuery] string rol, [FromQuery] int userId)
    {
        var result = await _adminService.EditarCuenta(dto, rol, userId);
        if (result.Success)
            return Ok(new { success = true, message = "Cuenta editada correctamente" });

        return BadRequest(new { success = false, message = result.ErrorMessage ?? "Error inesperado del lado del servidor" });
    }

    // API endpoint to assign role permissions to user
    [HttpPost("AssignRolePermissions")]
    public async Task<IActionResult> AssignRolePermissions([FromBody] AssignRolePermissionsDTO dto)
    {
        var result = await _adminService.AssignRolePermissionsToUser(dto.UserId, dto.Role);
        if (result.Success)
        {
            return Ok(new { success = true, message = $"Permisos del rol {dto.Role} asignados correctamente" });
        }
        return BadRequest(new { success = false, message = "Error al asignar los permisos del rol" });
    }

    // API endpoint to remove all permissions from user
    [HttpPost("RemoveAllPermissions")]
    public async Task<IActionResult> RemoveAllPermissions([FromBody] int userId)
    {
        var result = await _adminService.RemoveAllPermissionsFromUser(userId);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Todos los permisos removidos correctamente" });
        }
        return BadRequest(new { success = false, message = "Error al remover los permisos" });
    }

    // API endpoint to copy permissions from one user to another
    [HttpPost("CopyPermissions")]
    public async Task<IActionResult> CopyPermissions([FromBody] dynamic data)
    {
        int fromUserId = data.fromUserId;
        int toUserId = data.toUserId;
        
        var result = await _adminService.CopyPermissions(fromUserId, toUserId);
        if (result.Success)
        {
            return Ok(new { success = true, message = "Permisos copiados correctamente" });
        }
        return BadRequest(new { success = false, message = "Error al copiar los permisos" });
    }

    //API endpoint to create a group
    [RequiredPermission("permiso_grupo:create")]
    [HttpPost("CreateGroup")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO dto)
    {
        var result = await _adminService.CreateGroup(dto.UserIds,dto.PermissionIds,dto.Name,dto.Description);

        if(result.Success)
        return Ok(new{success = true, message = "Grupo creado con exito"});

        return BadRequest (new {success = false, message= result.ErrorMessage});
    }

    //API endppoint to erase a group
    [RequiredPermission("permiso_grupo:delete")]
    [HttpPost("EraseGroup/{groupId}")]
    public async Task<IActionResult> EraseGroup(int groupId)
    {
        var result = await _adminService.EraseGroup(groupId);
        if(result.Success)
        return Ok(new {success = true, message = "El grupo se ha eliminado correctamente"});

        return BadRequest(new{success = false, message = result.ErrorMessage});
    }
    
    //API endpoint to add a user to a group
    [RequiredPermission("permiso_grupo:manage_users")]
    [HttpPost("AddUsersToGroup/{groupId}")]
    public async Task<IActionResult> AddUsersToGroup(int groupId, [FromBody] int[] usersId)
    {
        var result = await _adminService.AddUsersToGroup(usersId,groupId);
        if(result.Success)
        return Ok(new {success = true, message = "El/Los usuario/s se ha/n agregado"});

        return BadRequest(new{success = false, message = result.ErrorMessage});
    }

    [RequiredPermission("permiso_grupo:manage_users")]
    [HttpPost("RemoveUsersFromGroup/{groupId}")]
    public async Task<IActionResult> RemoveUsersFromGroup(int groupId, [FromBody] int[] usersIds)
    {
        var result = await _adminService.RemoveUsersFromGroup(usersIds,groupId);
        if(result.Success)
        return Ok(new {success = true, message = "El/Los usuario/s se ha/n removido"});

        return BadRequest(new{success = false, message = result.ErrorMessage});
    }

    [RequiredPermission("permiso_grupo:manage_permissions")]
    [HttpPost("AddPermissionsToGroup/{groupId}")]
    public async Task<IActionResult> AddPermissionsToGroup(int groupId, [FromBody] int[] permissionsIds)
    {
        var result = await _adminService.AddPermissionsToGroup(permissionsIds,groupId);
        if(result.Success)
        return Ok(new {success = true, message = "El/Los permiso/s ha sido agregado/s"});

        return BadRequest(new{success = false, message = result.ErrorMessage});
    }

    [RequiredPermission("permiso_grupo:manage_permissions")]
    [HttpPost("RemovePermissionsFromGroup/{groupId}")]
    public async Task<IActionResult> RemovePermissionsFromGroup(int groupId, [FromBody] int[] permissionsIds)
    {
        var result = await _adminService.RemovePermissionsFromGroup(permissionsIds,groupId);
        if(result.Success)
        return Ok(new {success = true, message = "El/Los permiso/s ha sido removido/s"});

        return BadRequest(new{success = false, message = result.ErrorMessage});
    }

    // API endpoint to create a new user account from the admin panel
    [HttpPost("CreateAccount")]
    public async Task<IActionResult> CreateAccount([FromBody] RegisterDTO dto)
    {

        string requiredPerm = dto.Role.ToRolUsuario() switch 
        {
            RolUsuario.Paciente => "paciente:create",
            RolUsuario.Medico => "medico:create",
            RolUsuario.Secretaria => "secretaria:create",
            RolUsuario.Admin => "admin:create",
            _ => "persona:create"
        };

        if(! await _adminService.TienePermiso(UserId.Value, requiredPerm))
            return StatusCode(403, new { success = false, message = "No tienes permiso para crear este tipo de cuenta." });

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" | ", errors) });
        }

        var result = await _authService.RegisterAsync(dto);
        if (result.Success)
            return Ok(new { success = true, message = $"Cuenta creada correctamente para {result.UserName} ({result.Role})" });

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    // API endpoint to get role permissions
    [HttpGet("GetRolePermissions")]
    public async Task<IActionResult> GetRolePermissions()
    {
        var rolePermissions = await _adminService.GetRolePermissions();
        return Ok(rolePermissions);
    }

    // API endpoint to get all manual groups
    [HttpGet("GetManualGroups")]
    public async Task<IActionResult> GetManualGroups()
    {
        var groups = await _adminService.GetManualGroups();
        return Ok(groups);
    }

    // API endpoint to get group details by ID
    [HttpGet("GetGroupDetails/{groupId}")]
    public async Task<IActionResult> GetGroupDetails(int groupId)
    {
        var group = await _adminService.GetGroupDetails(groupId);
        if (group == null)
        {
            return NotFound(new { success = false, message = "Grupo no encontrado" });
        }
        return Ok(group);
    }

    // ─── Role Key Management ────────────────────────────────────────────────────

    // Returns which roles have a key set (never exposes the actual hash)
    [HttpGet("GetRoleKeys")]
    [RequiredPermission("role_key:update")]
    public async Task<IActionResult> GetRoleKeys()
    {
        var status = await _adminService.GetRoleKeysStatus();
        return Ok(status);
    }

    // Update the key for a specific role
    [HttpPost("UpdateRoleKey")]
    [RequiredPermission("role_key:update")]
    public async Task<IActionResult> UpdateRoleKey([FromBody] UpdateRoleKeyDTO dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" | ", errors) });
        }

        var result = await _adminService.UpdateRoleKey(dto.Role, dto.NewKey);
        if (result.Success)
            return Ok(new { success = true, message = $"Clave del rol '{dto.Role}' actualizada correctamente" });

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }
}