using MedCenter.Data;
using MedCenter.Models;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.Authentication.Components;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration configuration)
    {
        
        // Asegurar que la base de datos está creada y las migraciones aplicadas
        await context.Database.MigrateAsync();
        //await SeedRoleKeys(context,configuration);
        await SeedPermisos(context,configuration);
    }

    

    public static async Task SeedPermisos(AppDbContext context, IConfiguration configuration)
    {
        if(context.permisos.Any())
        return;

        var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var permisos = new Permiso[]
            {
                new Permiso
                {
                    Nombre = "turno:create",
                    Descripcion = "Agendar turnos",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "turno:manage",
                    Descripcion = "Gestionar turnos",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Manage
                },
                new Permiso
                {
                    Nombre = "turno:delete",
                    Descripcion = "Cancelar turnos",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "turno:delete_premium",
                    Descripcion = "Cancelar turnos con menos de 24 horas de antelacion",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "turno:edit",
                    Descripcion = "Reprogramar turnos",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Update,
                },
                new Permiso
                {
                    Nombre = "turno:edit_premium",
                    Descripcion = "Reprogramar turnos con menos de 24 hs de antelacion",
                    Recurso = MedCenter.Enums.Recurso.Turno,
                    Accion = MedCenter.Enums.AccionUsuario.Update
                },
                new Permiso
                {
                    Nombre = "medico:create",
                    Descripcion = "Crear medico",
                    Recurso = MedCenter.Enums.Recurso.Medico,
                    Accion = MedCenter.Enums.AccionUsuario.Create,
                },
                new Permiso
                {
                    Nombre = "medico:edit",
                    Descripcion = "Editar medico ",
                    Recurso = MedCenter.Enums.Recurso.Medico,
                    Accion = MedCenter.Enums.AccionUsuario.Update
                },
                new Permiso
                {
                    Nombre = "medico:delete",
                    Descripcion = "Desactivar medico",
                    Recurso = MedCenter.Enums.Recurso.Medico,
                    Accion = MedCenter.Enums.AccionUsuario.Update
                },
                new Permiso
                {
                    Nombre = "paciente:create",
                    Descripcion = "Crear paciente",
                    Recurso = MedCenter.Enums.Recurso.Paciente,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "paciente:edit",
                    Descripcion = "Editar paciente",
                    Recurso = MedCenter.Enums.Recurso.Paciente,
                    Accion = MedCenter.Enums.AccionUsuario.Update
                },
                new Permiso
                {
                    Nombre = "paciente:view",
                    Descripcion = "Ver lista de pacientes",
                    Recurso = MedCenter.Enums.Recurso.Paciente,
                    Accion = MedCenter.Enums.AccionUsuario.View
                },
                 new Permiso
                {
                    Nombre = "secretaria:create",
                    Descripcion = "Crear secretaria",
                    Recurso = MedCenter.Enums.Recurso.Secretaria,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                 new Permiso
                {
                    Nombre = "secretaria:edit",
                    Descripcion = "Editar secretaria",
                    Recurso = MedCenter.Enums.Recurso.Secretaria,
                    Accion = MedCenter.Enums.AccionUsuario.Update
                },
                new Permiso
                {
                    Nombre = "secretaria:delete",
                    Descripcion = "Desactivar secretaria",
                    Recurso = MedCenter.Enums.Recurso.Secretaria,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "reporte:create_audit_login",
                    Descripcion = "Crear reporte de auditoria de logins",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_turnos_resumen",
                    Descripcion = "Crear reporte resumido de turnos",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_audit_turno",
                    Descripcion = "Crear reporte de auditoria de turnos",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_ejecutivo",
                    Descripcion = "Crear reporte ejecutivo, con calculos",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_turnos_general",
                    Descripcion = "Crear reporte de todos los turnos",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_turnos_filtrado",
                    Descripcion = "Crear reporte de turnos con filtros especificos",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "reporte:create_entradas_creadas",
                    Descripcion = "Crear reporte sobre las entradas clinicas creadas desde la perspectiva del medico",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },new Permiso
                {
                    Nombre = "reporte:create_pacientes_todos",
                    Descripcion = "Crear reporte de todos los pacientes",
                    Recurso = MedCenter.Enums.Recurso.Reporte,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "rol:create",
                    Descripcion = "Crear rol",
                    Recurso = MedCenter.Enums.Recurso.Rol,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "rol:delete",
                    Descripcion = "Eliminar rol",
                    Recurso = MedCenter.Enums.Recurso.Rol,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "entrada_clinica:create",
                    Descripcion = "Crear entrada clinica",
                    Recurso = MedCenter.Enums.Recurso.EntradaClinica,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "historia_clinica:create",
                    Descripcion = "Crear historia clinica",
                    Recurso = MedCenter.Enums.Recurso.HistoriaClinica,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "permiso:create",
                    Descripcion = "Crear permiso",
                    Recurso = MedCenter.Enums.Recurso.Permiso,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                 new Permiso
                {
                    Nombre = "permiso:delete",
                    Descripcion = "Eliminar permiso",
                    Recurso = MedCenter.Enums.Recurso.Permiso,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "disponibilidad:view",
                    Descripcion = "Consultar la disponibilidad",
                    Recurso = MedCenter.Enums.Recurso.Disponibilidad,
                    Accion = MedCenter.Enums.AccionUsuario.View
                },
                new Permiso
                {
                    Nombre = "bloque_disponibilidad:create",
                    Descripcion = "Crear un bloque de disponibilidad",
                    Recurso = MedCenter.Enums.Recurso.BloqueDisponibilidad,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
                new Permiso
                {
                    Nombre = "bloque_disponibilidad:delete",
                    Descripcion = "Eliminar un bloque de disponibilidad",
                    Recurso = MedCenter.Enums.Recurso.BloqueDisponibilidad,
                    Accion = MedCenter.Enums.AccionUsuario.Delete
                },
                new Permiso
                {
                    Nombre = "slots_agenda:create",
                    Descripcion = "Generar slots de agenda de acuerdo con los bloques de disponibilidad activos",
                    Recurso = MedCenter.Enums.Recurso.SlotsAgenda,
                    Accion = MedCenter.Enums.AccionUsuario.Create
                },
            };
            await context.permisos.AddRangeAsync(permisos);

            await context.SaveChangesAsync();
            
            var permisosTracked = await context.permisos.ToListAsync();
            var dic = permisosTracked.ToDictionary(p => p.Nombre, p => p.Id);

            permisosTracked.FirstOrDefault(p => p.Nombre == "turno:delete_premium").PermisoPadreId = dic["turno:delete"];
            permisosTracked.FirstOrDefault(p => p.Nombre == "turno:edit_premium").PermisoPadreId = dic["turno:edit"];
            await context.SaveChangesAsync();


            var rolespermiso = new RolPermiso[]
            {
                //permisos medico
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Medico,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_entradas_creadas").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Medico,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_turnos_filtrado").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Medico,
                    PermisoId = permisos.Where(p => p.Nombre == "entrada_clinica:create").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Medico,
                    PermisoId = permisos.Where(p => p.Nombre == "historia_clinica:create").Select(p => p.Id).FirstOrDefault(),
                },
                //permisos paciente
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Paciente,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:create").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Paciente,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:manage").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Paciente,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:edit").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Paciente,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:delete").Select(p => p.Id).FirstOrDefault(),
                },
                //permisos secretaria
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:create").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:manage").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:edit").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:delete").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:delete_premium").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "turno:edit_premium").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_audit_login").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_audit_turno").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                  RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                  PermisoId = permisos.Where(p => p.Nombre == "reporte:create_turnos_resumen").Select(p => p.Id).FirstOrDefault()  
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_ejecutivo").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_turnos_general").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_turnos_filtrado").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "reporte:create_pacientes_todos").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "paciente:create").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "paciente:edit").Select(p => p.Id).FirstOrDefault(),
                },
                new RolPermiso
                {
                    RolNombre = MedCenter.Services.TurnoSv.RolUsuario.Secretaria,
                    PermisoId = permisos.Where(p => p.Nombre == "disponibilidad:view").Select(p => p.Id).FirstOrDefault(),
                },
               
            };

            await context.rolPermisos.AddRangeAsync(rolespermiso);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }



}