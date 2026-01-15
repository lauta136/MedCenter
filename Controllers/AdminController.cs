using MedCenter.DTOs;
using MedCenter.Services.AdminService;
using MedCenter.Services.TurnoSv;
using Microsoft.AspNetCore.Mvc;

namespace MedCenter.Controllers;


public class AdminController : BaseController
{
    private readonly AdminService _adminService;

    public AdminController(AdminService adminService)
    {
        _adminService = adminService;
    }
    
    public async Task<IActionResult> Index()
    {
        ViewBag.EsAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        return View();
    }

    // View for permission management panel
    public async Task<IActionResult> PanelAdmin()
    {
        var isAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        if (!isAdmin)
        {
            return RedirectToAction("AccessDenied", "Access");
        }
        ViewBag.EsAdmin = await _adminService.AccesoAPanelAdmin(UserId.Value);
        ViewBag.Rol = UserRole;
        var viewModel = await _adminService.GetPermissionManagementData();
        return View(viewModel);
    }

    // API endpoint to get all users with permissions
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersWithPermissions();
        return Json(users);
    }

    // API endpoint to get all permissions
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _adminService.GetAllPermissions();
        return Json(permissions);
    }

    // API endpoint to assign permission to user
    [HttpPost]
    public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDTO dto)
    {
        var result = await _adminService.AssignPermissionToUser(dto.UserId, dto.PermissionId);
        if (result)
        {
            return Json(new { success = true, message = "Permiso asignado correctamente" });
        }
        return Json(new { success = false, message = "Error al asignar el permiso" });
    }

    // API endpoint to remove permission from user
    [HttpPost]
    public async Task<IActionResult> RemovePermission([FromBody] RemovePermissionDTO dto)
    {
        var result = await _adminService.RemovePermissionFromUser(dto.UserId, dto.PermissionId);
        if (result)
        {
            return Json(new { success = true, message = "Permiso removido correctamente" });
        }
        return Json(new { success = false, message = "Error al remover el permiso" });
    }

    // API endpoint to assign role permissions to user
    [HttpPost]
    public async Task<IActionResult> AssignRolePermissions([FromBody] AssignRolePermissionsDTO dto)
    {
        var result = await _adminService.AssignRolePermissionsToUser(dto.UserId, dto.Role);
        if (result)
        {
            return Json(new { success = true, message = $"Permisos del rol {dto.Role} asignados correctamente" });
        }
        return Json(new { success = false, message = "Error al asignar los permisos del rol" });
    }

    // API endpoint to remove all permissions from user
    [HttpPost]
    public async Task<IActionResult> RemoveAllPermissions([FromBody] int userId)
    {
        var result = await _adminService.RemoveAllPermissionsFromUser(userId);
        if (result)
        {
            return Json(new { success = true, message = "Todos los permisos removidos correctamente" });
        }
        return Json(new { success = false, message = "Error al remover los permisos" });
    }

    // API endpoint to copy permissions from one user to another
    [HttpPost]
    public async Task<IActionResult> CopyPermissions([FromBody] dynamic data)
    {
        int fromUserId = data.fromUserId;
        int toUserId = data.toUserId;
        
        var result = await _adminService.CopyPermissions(fromUserId, toUserId);
        if (result)
        {
            return Json(new { success = true, message = "Permisos copiados correctamente" });
        }
        return Json(new { success = false, message = "Error al copiar los permisos" });
    }

    // API endpoint to assign permission to group of users
    [HttpPost]
    public async Task<IActionResult> AssignPermissionToGroup([FromBody] AssignPermissionToGroupDTO data)
    {
        var result = await _adminService.AssignPermissionToGroup(data.UsersIds, data.PermissionId);
        if (result)
        {
            return Json(new { success = true, message = "Permiso asignado correctamente" });
        }
        return Json(new { success = false, message = "Error al asignar el permiso" });
    }

    // API endpoint to remove permission from group of users
    [HttpPost]
    public async Task<IActionResult> RemovePermissionFromGroup([FromBody] RemovePermissionFromGroupDTO data)
    {
        var result = await _adminService.RemovePermissionFromGroup(data.UsersIds, data.PermissionId);
        if (result)
        {
            return Json(new { success = true, message = "Permiso removido correctamente" });
        }
        return Json(new { success = false, message = "Error al remover el permiso" });
    }

    // API endpoint to get role permissions
    [HttpGet]
    public async Task<IActionResult> GetRolePermissions()
    {
        var rolePermissions = await _adminService.GetRolePermissions();
        return Json(rolePermissions);
    }
}