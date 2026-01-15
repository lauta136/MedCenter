using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Enums;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.AdminService;

public class AdminService
{
    private readonly AppDbContext _context;
    public AdminService(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }
    
    public async Task<bool> AccesoAPanelAdmin(int id)
    {
        int [] permitsIds = await _context.personaPermisos.Where(pp => pp.PersonaId == id).Select(pp => pp.PermisoId).ToArrayAsync();
        var adminIds = await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Admin).Select(rp => rp.PermisoId).ToListAsync();

        return permitsIds.Any(id => adminIds.Contains(id));
    }

    // Get all users with their permissions
    public async Task<List<UserPermissionDTO>> GetAllUsersWithPermissions()
    {
        var users = await _context.personas
            .Include(p => p.PersonaPermisos)
                .ThenInclude(pp => pp.Permiso)
            .Select(p => new UserPermissionDTO
            {
                UserId = p.id,
                UserName = p.nombre,
                Email = p.email,
                Permissions = p.PersonaPermisos.Select(pp => new PermissionDTO
                {
                    Id = pp.Permiso.Id,
                    Nombre = pp.Permiso.Nombre,
                    Descripcion = pp.Permiso.Descripcion,
                    Recurso = pp.Permiso.Recurso,
                    Accion = pp.Permiso.Accion
                }).ToList()
            })
            .ToListAsync();

        // Get roles for each user
        foreach (var user in users)
        {
            user.Roles = await GetUserRoles(user.UserId);
        }

        return users;
    }

    // Get all available permissions
    public async Task<List<PermissionDTO>> GetAllPermissions()
    {
        return await _context.permisos
            .Select(p => new PermissionDTO
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Recurso = p.Recurso,
                Accion = p.Accion
            })
            .ToListAsync();
    }

    // Get users grouped by their permission sets
    public async Task<List<PermissionGroupDTO>> GetUsersGroupedByPermissions()
    {
        var users = await GetAllUsersWithPermissions();
        
        var groups = users
            .GroupBy(u => string.Join(",", u.Permissions.OrderBy(p => p.Id).Select(p => p.Id)))
            .Select(g => new PermissionGroupDTO
            {
                GroupKey = g.Key,
                Permissions = g.First().Permissions,
                Users = g.ToList()
            })
            .OrderByDescending(g => g.Users.Count)
            .ToList();

        return groups;
    }

    // Get permissions for each role
    public async Task<Dictionary<RolUsuario, List<PermissionDTO>>> GetRolePermissions()
    {
        var rolePermissions = new Dictionary<RolUsuario, List<PermissionDTO>>();
        
        var roles = Enum.GetValues<RolUsuario>().Where(r => r != RolUsuario.System);
        
        foreach (var role in roles)
        {
            var permissions = await _context.rolPermisos
                .Where(rp => rp.RolNombre == role)
                .Include(rp => rp.Permiso)
                .Select(rp => new PermissionDTO
                {
                    Id = rp.Permiso.Id,
                    Nombre = rp.Permiso.Nombre,
                    Descripcion = rp.Permiso.Descripcion,
                    Recurso = rp.Permiso.Recurso,
                    Accion = rp.Permiso.Accion
                })
                .ToListAsync();
            
            rolePermissions[role] = permissions;
        }

        return rolePermissions;
    }

    // Assign a permission to a user
    public async Task<bool> AssignPermissionToUser(int userId, int permissionId)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.personas.AnyAsync(p => p.id == userId);
            if (!userExists) return false;

            // Check if permission exists
            var permissionExists = await _context.permisos.AnyAsync(p => p.Id == permissionId);
            if (!permissionExists) return false;

            // Check if already assigned
            var alreadyAssigned = await _context.personaPermisos
                .AnyAsync(pp => pp.PersonaId == userId && pp.PermisoId == permissionId);
            
            if (alreadyAssigned) return true;

            // Assign permission
            _context.personaPermisos.Add(new PersonaPermiso
            {
                PersonaId = userId,
                PermisoId = permissionId
            });

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Remove a permission from a user
    public async Task<bool> RemovePermissionFromUser(int userId, int permissionId)
    {
        try
        {
            var personaPermiso = await _context.personaPermisos
                .FirstOrDefaultAsync(pp => pp.PersonaId == userId && pp.PermisoId == permissionId);

            if (personaPermiso == null) return false;

            _context.personaPermisos.Remove(personaPermiso);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Assign all permissions from a role to a user
    public async Task<bool> AssignRolePermissionsToUser(int userId, RolUsuario role)
    {
        try
        {
            var rolePermissions = await _context.rolPermisos
                .Where(rp => rp.RolNombre == role)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            foreach (var permissionId in rolePermissions)
            {
                await AssignPermissionToUser(userId, permissionId);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Remove all permissions from a user
    public async Task<bool> RemoveAllPermissionsFromUser(int userId)
    {
        try
        {
            var userPermissions = await _context.personaPermisos
                .Where(pp => pp.PersonaId == userId)
                .ToListAsync();

            _context.personaPermisos.RemoveRange(userPermissions);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    //assign permission to a group of users
    public async Task<bool> AssignPermissionToGroup(List<int> usersIds, int permissionId)
    {
        try
        {
            // Check if permission exists
            var permissionExists = await _context.permisos.AnyAsync(p => p.Id == permissionId);
            if (!permissionExists) return false;

            foreach(int i in usersIds)
            {
                // Check if user exists
                var userExists = await _context.personas.AnyAsync(p => p.id == i);
                if (!userExists) return false;

                // Check if already assigned
                var alreadyAssigned = await _context.personaPermisos
                .AnyAsync(pp => pp.PersonaId == i && pp.PermisoId == permissionId);
            
                if (alreadyAssigned) continue;

                 // Assign permission
                _context.personaPermisos.Add(new PersonaPermiso
                {
                    PersonaId = i,
                    PermisoId = permissionId
                });
            }
          
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Remove a permission from a group of users 
    public async Task<bool> RemovePermissionFromGroup(List<int> usersIds, int permissionId)
    {
        try
        {
            foreach(int i in usersIds)
            {
                var personaPermiso = await _context.personaPermisos
                .FirstOrDefaultAsync(pp => pp.PersonaId == i && pp.PermisoId == permissionId);

                if (personaPermiso == null) continue;

                _context.personaPermisos.Remove(personaPermiso);
            }
           
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Get user roles based on their permissions
    private async Task<List<RolUsuario>> GetUserRoles(int userId)
    {
        if(await _context.pacientes.AnyAsync(p => p.id == userId))
        return new List<RolUsuario> {RolUsuario.Paciente};

        if(await _context.medicos.AnyAsync(p => p.id == userId))
        return new List<RolUsuario> {RolUsuario.Medico};

        var userPermissionIds = await _context.personaPermisos
            .Where(pp => pp.PersonaId == userId)
            .Select(pp => pp.PermisoId)
            .ToListAsync();

        var roles = new List<RolUsuario>();
        
        foreach (var role in Enum.GetValues<RolUsuario>())
        {
            if (role == RolUsuario.System) continue;

            var rolePermissionIds = await _context.rolPermisos
                .Where(rp => rp.RolNombre == role && rp.RolNombre != RolUsuario.Paciente&& rp.RolNombre != RolUsuario.Medico)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            // Check if user has all permissions for this role
            if (rolePermissionIds.All(rpId => userPermissionIds.Contains(rpId)) && rolePermissionIds.Any())
            {
                roles.Add(role);
            }
        }

        return roles;
    }

    // Get complete permission management data
    public async Task<PermissionManagementViewModel> GetPermissionManagementData()
    {
        return new PermissionManagementViewModel
        {
            AllUsers = await GetAllUsersWithPermissions(),
            AllPermissions = await GetAllPermissions(),
            PermissionGroups = await GetUsersGroupedByPermissions(),
            RolePermissions = await GetRolePermissions()
        };
    }

    // Copy permissions from one user to another
    public async Task<bool> CopyPermissions(int fromUserId, int toUserId)
    {
        try
        {
            var sourcePermissions = await _context.personaPermisos
                .Where(pp => pp.PersonaId == fromUserId)
                .Select(pp => pp.PermisoId)
                .ToListAsync();

            foreach (var permissionId in sourcePermissions)
            {
                await AssignPermissionToUser(toUserId, permissionId);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}