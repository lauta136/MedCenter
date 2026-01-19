using System.Data.Common;
using DocumentFormat.OpenXml.Wordprocessing;
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
    
    public async Task<AuthResult> AccesoAPanelAdmin(int id)
    {
        try
        {
            int [] permitsIds = await _context.personaPermisos.Where(pp => pp.PersonaId == id).Select(pp => pp.PermisoId).ToArrayAsync();
            var adminIds = await _context.rolPermisos.Where(rp => rp.RolNombre == RolUsuario.Admin).Select(rp => rp.PermisoId).ToListAsync();

            bool hasAccess = permitsIds.Any(id => adminIds.Contains(id));
            return new AuthResult { Success = hasAccess };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error al verificar acceso: {ex.Message}" };
        }
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

    //Create a group manually
    public async Task<AuthResult> CreateGroup(int [] usersIds, int [] permissionsIds, string name, string? description)
    {
        if(await _context.gruposPermisosPersonas.AnyAsync(g => g.Nombre.ToLower() == name.ToLower()))
        return new AuthResult{Success = false, ErrorMessage="El nombre de grupo ya esta en uso"};

        var users = await _context.personas.Where(p => usersIds.Contains(p.id)).ToListAsync();
        var permissions = await _context.permisos.Where(p => permissionsIds.Contains(p.Id)).ToListAsync();
        var personasGrupos = new List<PersonaGrupo>();
        var permisosGrupos = new List<PermisoGrupo>();

        var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var grupo = new GrupoPermisosPersonas
            {
                Nombre = name,
                Descripcion = description ?? string.Empty,
                FechaCreacion = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            _context.gruposPermisosPersonas.Add(grupo);
            await _context.SaveChangesAsync();

            foreach(var user in users)
            {
                var personaGrupo = new PersonaGrupo
                {
                    GrupoId = grupo.Id,
                    PersonaId = user.id
                };
                personasGrupos.Add(personaGrupo);
            }

            foreach(var permission in permissions)
            {
                var permisoGrupo = new PermisoGrupo
                {
                    GrupoId = grupo.Id,
                    PermisoId = permission.Id
                };
                permisosGrupos.Add(permisoGrupo);
            }
            
            await _context.AddRangeAsync(personasGrupos);
            await _context.AddRangeAsync(permisosGrupos);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AuthResult{Success = true};
            
        }
        catch
        {
            await transaction.RollbackAsync();
            return new AuthResult{Success = false, ErrorMessage = "Error en la carga de el nuevo grupo"};
        }
        
    }

    public async Task<AuthResult> EraseGroup(int id)
    {
        try
        {
            var grupo = await _context.gruposPermisosPersonas
                .FirstOrDefaultAsync(g => g.Id == id);
        
            if (grupo == null)
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "El grupo no existe" 
                };
        
            _context.gruposPermisosPersonas.Remove(grupo);
            await _context.SaveChangesAsync();
        
            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = $"Error al eliminar el grupo: {ex.Message}" 
            };
        }
    }

    public async Task<AuthResult> AddUserToGroup(int userId, int groupId)
    {
        try
        {
            await _context.personasGrupos.AddAsync(new PersonaGrupo
            {
                PersonaId = userId,
                GrupoId = groupId
            });

            await _context.SaveChangesAsync();
            return new AuthResult{Success = true};
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;

            if(innerMessage.Contains("duplicate key"))
            return new AuthResult{Success = false, ErrorMessage = "El usuario ya pertenece a este grupo"};
            if(innerMessage.Contains("foreign key") && innerMessage.Contains("grupo"))
            return new AuthResult{Success = false, ErrorMessage = "El grupo no se encontro"};
            if(innerMessage.Contains("foreign key") && innerMessage.Contains("persona"))
            return new AuthResult{Success = false, ErrorMessage = "El usuario no se encontro"};

            return new AuthResult{Success = false, ErrorMessage = $"Error al agregar usuario al grupo: {ex.Message}"};
        }
    }

    public async Task<AuthResult> RemoveUserFromGroup(int userId, int groupId)
    {
        try
        {
            if(!await _context.personas.AnyAsync(p => p.id == userId))
            return new AuthResult{Success = false, ErrorMessage = "El usuario a eliminar al grupo no se encontro"};

            if(!await _context.gruposPermisosPersonas.AnyAsync(p => p.Id == groupId))
            return new AuthResult{Success = false, ErrorMessage = "El grupo no se encontro"};

            _context.personasGrupos.Remove(new PersonaGrupo
            {
                PersonaId = userId,
                GrupoId = groupId
            });

            await _context.SaveChangesAsync();
            return new AuthResult{Success = true};
        }
        catch (Exception ex)
        {
            return new AuthResult{Success = false, ErrorMessage = $"Error al remover usuario de; grupo: {ex.Message}"};
        }
    }

    public async Task<AuthResult> AddPermissionToGroup(int permissionId, int groupId)
    {

        try
        {
            await _context.permisosGrupos.AddAsync(new PermisoGrupo
            {
                PermisoId = permissionId,
                GrupoId = groupId
            });

            await _context.SaveChangesAsync();
            return new AuthResult{Success = true};
        }
        catch(DbUpdateException ex)
        {
              // Handle specific database errors
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
        
            if (innerMessage.Contains("duplicate key") || innerMessage.Contains("unique constraint"))
                return new AuthResult { Success = false, ErrorMessage = "Esta relación ya existe" };
        
            if (innerMessage.Contains("foreign key") && innerMessage.Contains("permiso"))
                return new AuthResult { Success = false, ErrorMessage = "El permiso no existe" };
        
            if (innerMessage.Contains("foreign key") && innerMessage.Contains("grupo"))
                return new AuthResult { Success = false, ErrorMessage = "El grupo no existe" };

            return new AuthResult{Success = false, ErrorMessage = "Error al intentar hacer el cambio en la base de datos"};
        }
        
    }

    public async Task<AuthResult> RemovePermissionFromGroup(int permissionId, int groupId)
    {
        try
        {
            var count = await _context.permisosGrupos.Where(pg => pg.GrupoId == groupId && pg.PermisoId == permissionId).ExecuteDeleteAsync();
            if(count == 0)
            return new AuthResult{Success = false, ErrorMessage = "No se elimino el registro correctamente"};

            return new AuthResult{Success = true};
        }
        catch
        {
            return new AuthResult{Success = false, ErrorMessage = "Error al intentar hacer el cambio en la base de datos"};
        }
        
    }


    // Assign a permission to a user
    public async Task<AuthResult> AssignPermissionToUser(int userId, int permissionId)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.personas.AnyAsync(p => p.id == userId);
            if (!userExists) return new AuthResult { Success = false, ErrorMessage = "Usuario no encontrado" };

            // Check if permission exists
            var permissionExists = await _context.permisos.AnyAsync(p => p.Id == permissionId);
            if (!permissionExists) return new AuthResult { Success = false, ErrorMessage = "Permiso no encontrado" };

            // Check if already assigned
            var alreadyAssigned = await _context.personaPermisos
                .AnyAsync(pp => pp.PersonaId == userId && pp.PermisoId == permissionId);
            
            if (alreadyAssigned) return new AuthResult { Success = true };

            // Assign permission
            _context.personaPermisos.Add(new PersonaPermiso
            {
                PersonaId = userId,
                PermisoId = permissionId
            });

            await _context.SaveChangesAsync();
            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error al asignar permiso: {ex.Message}" };
        }
    }

    // Remove a permission from a user
    public async Task<AuthResult> RemovePermissionFromUser(int userId, int permissionId)
    {
        try
        {
            var personaPermiso = await _context.personaPermisos
                .FirstOrDefaultAsync(pp => pp.PersonaId == userId && pp.PermisoId == permissionId);

            if (personaPermiso == null) return new AuthResult{Success = false, ErrorMessage = "No se encontro el vinculo entre usuario y permiso"};

            _context.personaPermisos.Remove(personaPermiso);
            await _context.SaveChangesAsync();
            return new AuthResult{Success = true};
        }
        catch
        {
            return new AuthResult{Success = false, ErrorMessage = "Error al intentar hacer el cambio en la base de datos"};
        }
    }

    // Assign all permissions from a role to a user
    public async Task<AuthResult> AssignRolePermissionsToUser(int userId, RolUsuario role)
    {
        try
        {
            var rolePermissions = await _context.rolPermisos
                .Where(rp => rp.RolNombre == role)
                .Select(rp => rp.PermisoId)
                .ToListAsync();

            foreach (var permissionId in rolePermissions)
            {
                var result = await AssignPermissionToUser(userId, permissionId);
                if (!result.Success) return result;
            }

            return new AuthResult{Success = true};
        }
        catch (Exception ex)
        {
            return new AuthResult{Success = false, ErrorMessage = $"Error al asignar permisos del rol: {ex.Message}"};
        }
    }

    // Remove all permissions from a user
    public async Task<AuthResult> RemoveAllPermissionsFromUser(int userId)
    {
        try
        {
            var userPermissions = await _context.personaPermisos
                .Where(pp => pp.PersonaId == userId)
                .ToListAsync();

            _context.personaPermisos.RemoveRange(userPermissions);
            await _context.SaveChangesAsync();
            return new AuthResult{Success = true};
        }
        catch
        {
            return new AuthResult{Success = false, ErrorMessage = "Error al intentar hacer el cambio en la base de datos"};
        }
    }

    //assign permission to a group of users, no a los grupos manuales sino los que estaban antes, ver si quitar
    public async Task<AuthResult> AssignPermissionToGroup(List<int> usersIds, int permissionId)
    {
        try
        {
            // Check if permission exists
            var permissionExists = await _context.permisos.AnyAsync(p => p.Id == permissionId);
            if (!permissionExists) return new AuthResult { Success = false, ErrorMessage = "Permiso no encontrado" };

            foreach(int i in usersIds)
            {
                // Check if user exists
                var userExists = await _context.personas.AnyAsync(p => p.id == i);
                if (!userExists) return new AuthResult { Success = false, ErrorMessage = $"Usuario {i} no encontrado" };

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
            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error al asignar permiso al grupo: {ex.Message}" };
        }
    }

    // Remove a permission from a group of users, NO A LOS GRUPOS NUEVOS, SOLO A LOS VIEJOS PROBABLEMENTE SACAR
    public async Task<AuthResult> RemovePermissionFromGroup(List<int> usersIds, int permissionId)
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
            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error al remover permiso del grupo: {ex.Message}" };
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
    public async Task<AuthResult> CopyPermissions(int fromUserId, int toUserId)
    {
        try
        {
            var sourcePermissions = await _context.personaPermisos
                .Where(pp => pp.PersonaId == fromUserId)
                .Select(pp => pp.PermisoId)
                .ToListAsync();

            foreach (var permissionId in sourcePermissions)
            {
                var result = await AssignPermissionToUser(toUserId, permissionId);
                if (!result.Success) return result;
            }

            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, ErrorMessage = $"Error al copiar permisos: {ex.Message}" };
        }
    }
}