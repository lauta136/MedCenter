using System.Data.Common;
using System.Transactions;
using DocumentFormat.OpenXml.Wordprocessing;
using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Enums;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Pkcs;

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
        var users = await _context.personas.Where(p => usersIds.Contains(p.id)).Include(p => p.PersonaPermisos).ToListAsync();
        var PermissionsIds = await _context.permisos.Where(p => permissionsIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
        var personasGrupos = new List<PersonaGrupo>();
        var permisosGrupos = new List<PermisoGrupo>();
        var permisosPersona = new List<PersonaPermiso>();

        using var transaction = await _context.Database.BeginTransactionAsync();
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

                foreach(int id in PermissionsIds)
                {
                    if(!user.PersonaPermisos.Any(pp => pp.PermisoId == id))
                    {
                        var personaPermiso = new PersonaPermiso
                        {
                            PermisoId = id,
                            GrupoId = grupo.Id,
                            Origen = PermisoSource.Group
                        };
                        permisosPersona.Add(personaPermiso);
                    }    
                }
            }

            foreach(var permissionId in PermissionsIds)
            {
                var permisoGrupo = new PermisoGrupo
                {
                    GrupoId = grupo.Id,
                    PermisoId = permissionId,
                };
                permisosGrupos.Add(permisoGrupo);

            }
            
            _context.AddRange(personasGrupos);
            _context.AddRange(permisosGrupos);
            _context.AddRange(permisosPersona);


            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AuthResult{Success = true};
            
        }
        catch(DbUpdateException e)
        {
            await transaction.RollbackAsync();
            var message = e.InnerException?.Message ?? e.Message;
            if(message.Contains("2305"))//codigo de violacion de unicidad
            {
                return new AuthResult{Success = false, ErrorMessage = "Este nombre ya esta siendo usado para otro grupo"};
            }
            return new AuthResult{Success = false, ErrorMessage = "Error en la carga de el nuevo grupo"}; 
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
            var grupo = await _context.gruposPermisosPersonas.Include(g => g.Permisos)
                //.Include(g => g.Personas)
                //.ThenInclude(p => p.Persona)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (grupo == null)
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "El grupo no existe" 
                };

            //var personasIds = await _context.personasGrupos.Where(pg => pg.GrupoId == grupo.Id).Select(pg => pg.PersonaId).ToListAsync();

            //await _context.personaPermisos.Where(pp => pp.GrupoId.Equals(grupo.Id) && personasIds.Contains(pp.PersonaId)).ExecuteDeleteAsync();

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

    public async Task<AuthResult> AddUsersToGroup(int[] usersIds, int groupId)
    {
        //var transaction = await _context.Database.BeginTransactionAsync();
        try
        {   
            var permisosIds= await _context.permisosGrupos.Where(pg =>pg.GrupoId == groupId).Select(pg => pg.PermisoId).ToArrayAsync();
            
            if(permisosIds.Length == 0)
            return new AuthResult{Success = false, ErrorMessage = "No se encontraron los permisos en la db"};

            var personasGrupos = usersIds.Select(u => new PersonaGrupo
            {
                PersonaId = u,
                GrupoId = groupId
            }).ToList();

             // Build all PersonaPermiso entries at once
            /*var personaPermisos = usersIds
            .SelectMany(userId => permisosIds.Select(permisoId => new PersonaPermiso
            {
                PermisoId = permisoId,
                PersonaId = userId,
                Origen = PermisoSource.Group,
                GrupoId = groupId
            }))
            .ToList();
            */

           

            // Get existing permissions for these users to avoid duplicates
            var existingPermissions = await _context.personaPermisos
            .Where(pp => usersIds.Contains(pp.PersonaId) && permisosIds.Contains(pp.PermisoId))
            .Select(pp => new { pp.PersonaId, pp.PermisoId })
            .ToHashSetAsync();

            /*var personasPermisos = usersIds.SelectMany(uI => permisosIds
            .Where(permisoId => !existingPermissions.Contains(new{PersonaId,uI})));
            */

            // Build PersonaPermiso entries, excluding duplicates
            var personaPermisos = usersIds
            .SelectMany(userId => permisosIds
                .Where(permisoId => !existingPermissions.Contains(new { PersonaId = userId, PermisoId = permisoId }))
                .Select(permisoId => new PersonaPermiso
                {
                    PermisoId = permisoId,
                    PersonaId = userId,
                    Origen = PermisoSource.Group,
                    GrupoId = groupId
                }))
            .ToList();
            
            _context.personasGrupos.AddRange(personasGrupos);
            _context.personaPermisos.AddRange(personaPermisos);

            await _context.SaveChangesAsync();
            
            //await transaction.CommitAsync();
            return new AuthResult{Success = true};
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            //await transaction.RollbackAsync();

            if(innerMessage.Contains("duplicate key"))
            return new AuthResult{Success = false, ErrorMessage = "Un usuario ya pertenece a este grupo"};
            if(innerMessage.Contains("foreign key") && innerMessage.Contains("grupo"))
            return new AuthResult{Success = false, ErrorMessage = "El grupo no se encontro"};
            if(innerMessage.Contains("foreign key") && innerMessage.Contains("persona"))
            return new AuthResult{Success = false, ErrorMessage = "Un usuario no se encontro"};

            return new AuthResult{Success = false, ErrorMessage = $"Error al agregar usuario al grupo: {ex.Message}"};
        }
    }

    public async Task<AuthResult> RemoveUsersFromGroup(int[] usersIds, int groupId)
    {
        using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            if(!await _context.gruposPermisosPersonas.AnyAsync(p => p.Id == groupId))
            return new AuthResult{Success = false, ErrorMessage = "El grupo no se encontro"};

            var count = await _context.personasGrupos.Where(pg => usersIds.Contains(pg.PersonaId) && pg.GrupoId == groupId).ExecuteDeleteAsync();
            if(count == 0)
            return new AuthResult{Success = false, ErrorMessage = "No se encontraron los usuarios seleccionados dentro del grupo"};

            var permToDelete = new List<PersonaPermiso>();

            foreach(var user in usersIds)
            {
                var affectedPerm = await _context.personaPermisos.Where(pp => pp.PersonaId == user && pp.GrupoId == groupId).ToListAsync();

                var permIds = affectedPerm.Select(ap => ap.PermisoId);

                var alternatives = await _context.personasGrupos.Where(pg => pg.PersonaId == user)
                                    .Join(_context.permisosGrupos
                                        .Where(pg => permIds
                                            .Contains(pg.PermisoId)), persGrup => persGrup.GrupoId, permGrup => permGrup.GrupoId,
                                        (persGrup, permGrup) => new {permGrup.GrupoId,permGrup.PermisoId})
                                    .ToListAsync();

                foreach(var perm in affectedPerm)
                {
                    if(alternatives.Any(a => a.PermisoId == perm.PermisoId))
                    {
                        var aux = alternatives.FirstOrDefault(a => a.PermisoId == perm.PermisoId);
                        perm.GrupoId = aux.GrupoId;
                    }
                    else
                    {
                        permToDelete.Add(perm);
                    }
                }
            }

            _context.personaPermisos.RemoveRange(permToDelete);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return new AuthResult{Success = true};
        }
        catch (Exception ex)
        {
            await transaccion.RollbackAsync();
            return new AuthResult{Success = false, ErrorMessage = $"Error al remover usuario del grupo: {ex.Message}"};
        }
    }

    public async Task<AuthResult> AddPermissionsToGroup(int []permissionsIds, int groupId)
    {
        //using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {   
            var permisosNuevos = permissionsIds.Select(p => new PermisoGrupo
            {
                PermisoId = p,
                GrupoId = groupId
            });
            var miembros = await _context.personasGrupos.Where(pg => pg.GrupoId == groupId).Select(pg => pg.PersonaId).ToListAsync();
            
            var permisosActuales = await _context.personaPermisos.Where(pp => miembros.Contains(pp.PersonaId))
                                    .Select(pp => new{PermisoId = pp.PermisoId,PersonaId = pp.PersonaId}).ToHashSetAsync();

            // Create PersonaPermiso entries (only for permissions members don't have)
            var permisosPersonasNuevos = miembros
            .SelectMany(miembroId => permissionsIds
                .Where(permId => !permisosActuales.Contains(new { PermisoId = permId, PersonaId = miembroId, }))
                .Select(permId => new PersonaPermiso
                {
                    PersonaId = miembroId,
                    PermisoId = permId,
                    GrupoId = groupId,
                    Origen = PermisoSource.Group
                }))
            .ToList();

            _context.permisosGrupos.AddRange(permisosNuevos);
            _context.personaPermisos.AddRange(permisosPersonasNuevos);
            await _context.SaveChangesAsync();
            //await transaction.CommitAsync();

            return new AuthResult{Success = true};
        }
        catch(DbUpdateException ex)
        {
            // Handle specific database errors
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            //await transaction.RollbackAsync();
            if (innerMessage.Contains("duplicate key") || innerMessage.Contains("unique constraint"))
                return new AuthResult { Success = false, ErrorMessage = "Alguno de los permisos ya existe en este grupo" };
        
            if (innerMessage.Contains("foreign key") && innerMessage.Contains("permiso"))
                return new AuthResult { Success = false, ErrorMessage = "El permiso no existe" };
        
            if (innerMessage.Contains("foreign key") && innerMessage.Contains("grupo"))
                return new AuthResult { Success = false, ErrorMessage = "El grupo no existe" };

            return new AuthResult{Success = false, ErrorMessage = "Error al intentar hacer el cambio en la base de datos"};
        }
        
    }

    public async Task<AuthResult> RemovePermissionsFromGroup(int[] permissionsIds, int groupId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Get all members of this group
            var groupMembers = await _context.personasGrupos
                .Where(pg => pg.GrupoId == groupId)
                .Select(pg => pg.PersonaId)
                .ToListAsync();

            if (groupMembers.Count == 0)
                return new AuthResult { Success = false, ErrorMessage = "El grupo no tiene miembros" };

            // Get PersonaPermiso entries that need to be handled
            var affectedPermissions = await _context.personaPermisos
                .Where(pp => groupMembers.Contains(pp.PersonaId) 
                    && permissionsIds.Contains(pp.PermisoId) 
                    && pp.GrupoId == groupId)
                .ToListAsync();

            // For each affected permission, check if user has it from another group
            foreach (var pp in affectedPermissions)
            {
                // Find alternative group that has this permission for this user
                var alternativeGroupId = await _context.personasGrupos
                    .Where(pg => pg.PersonaId == pp.PersonaId && pg.GrupoId != groupId)
                    .Join(_context.permisosGrupos,
                        pg => pg.GrupoId,
                        pmg => pmg.GrupoId,
                        (pg, pmg) => new { pg.GrupoId, pmg.PermisoId })
                    .Where(x => x.PermisoId == pp.PermisoId)
                    .Select(x => x.GrupoId)
                    .FirstOrDefaultAsync();

                if (alternativeGroupId != 0)
                {
                    // Reassign to another group
                    pp.GrupoId = alternativeGroupId;
                }
                else
                {
                    // No alternative, remove permission
                    _context.personaPermisos.Remove(pp);
                }
            }

            // Remove permissions from the group itself
            var count = await _context.permisosGrupos
                .Where(pg => pg.GrupoId == groupId && permissionsIds.Contains(pg.PermisoId))
                .ExecuteDeleteAsync();

            if (count == 0)
            {
                await transaction.RollbackAsync();
                return new AuthResult { Success = false, ErrorMessage = "No se encontraron los permisos en el grupo" };
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new AuthResult { Success = false, ErrorMessage = $"Error al remover permisos del grupo: {ex.Message}" };
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

    // Get all manual groups with their counts
    public async Task<List<object>> GetManualGroups()
    {
        var groups = await _context.gruposPermisosPersonas
            .Select(g => new
            {
                id = g.Id,
                name = g.Nombre,
                description = g.Descripcion,
                userCount = _context.personasGrupos.Count(pg => pg.GrupoId == g.Id),
                permissionCount = _context.permisosGrupos.Count(pg => pg.GrupoId == g.Id)
            })
            .ToListAsync();

        return groups.Cast<object>().ToList();
    }

    // Get group details including users and permissions
    public async Task<object?> GetGroupDetails(int groupId)
    {
        var group = await _context.gruposPermisosPersonas
            .Where(g => g.Id == groupId)
            .Select(g => new
            {
                id = g.Id,
                name = g.Nombre,
                description = g.Descripcion,
                users = _context.personasGrupos
                    .Where(pg => pg.GrupoId == g.Id)
                    .Select(pg => new
                    {
                        userId = pg.PersonaId,
                        userName = pg.Persona.nombre,
                        email = pg.Persona.email
                    })
                    .ToList(),
                permissions = _context.permisosGrupos
                    .Where(pg => pg.GrupoId == g.Id)
                    .Select(pg => new
                    {
                        id = pg.Permiso.Id,
                        nombre = pg.Permiso.Nombre,
                        descripcion = pg.Permiso.Descripcion,
                        recurso = pg.Permiso.Recurso,
                        accion = pg.Permiso.Accion
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return group;
    }
}