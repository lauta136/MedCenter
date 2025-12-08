using MedCenter.Data;
using MedCenter.Models;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MedCenter.DTOs;
using MedCenter.Services.DisponibilidadMedico;
using System.Drawing;
using Microsoft.EntityFrameworkCore.Design;
using MedCenter.Services.TurnoStates;
using System.Runtime.Serialization;
using MedCenter.Migrations;
using DocumentFormat.OpenXml.Office.CustomUI;
using MedCenter.Extensions;
using System.Security.Claims;

namespace MedCenter.Services.TurnoSv;

public class TurnoService
{
    private readonly AppDbContext _context;
    private readonly TurnoStateService _stateService;
    private readonly DisponibilidadService _dispoService;

    private readonly IHttpContextAccessor _httpContext;
    public TurnoService(AppDbContext appDbContext, TurnoStateService turnoStateService, DisponibilidadService disponibilidadService,IHttpContextAccessor httpContextAccessor)
    {
        _context = appDbContext;
        _stateService = turnoStateService;
        _dispoService = disponibilidadService;
        _httpContext = httpContextAccessor;
    }

    public int? GetCurrentUserId()
    {
        return int.TryParse(_httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int result) ? result : null;
    }

    public string? GetCurrentUserName()
    {
        return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public RolUsuario? GetCurrentUserRole()
    {
        return _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value.ToRolUsuario();
    }
    public async Task<(bool success, string message)> Reservar(ReservarTurnoDTO dto, int? pacienteElegidoId)
    {
        var currentUserRole = GetCurrentUserRole()!.Value;
        var currentUserId = GetCurrentUserId()!.Value;
        var currentUserName = GetCurrentUserName()!;

        if (!await _dispoService.SlotEstaDisponible(dto.SlotId))
            return (false, "El horario ya no esta disponible") ;//Los Json se crean de forma dinamica

        var slot = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == dto.SlotId);

        if (slot == null)
            return (false,"No se encontro el horario");

        if(slot.fecha <= DateOnly.FromDateTime(DateTime.Now)) 
            return (false,  "No pueden reservarse turnos para el dia actual o hacia el pasado, hagalo con mas antelacion");
        
        int pacienteFinalId;

        if (currentUserRole == RolUsuario.Secretaria)
        {
            if (!pacienteElegidoId.HasValue)
            {
                return (false, "Debe elegir un paciente");
            }
            pacienteFinalId = pacienteElegidoId.Value;
        }
        else
        {
            pacienteFinalId = currentUserId;
        }

        var turno = new Turno
        {
            slot_id = dto.SlotId,
            fecha = slot.fecha,
            hora = slot.horainicio,
            paciente_id = pacienteFinalId,
            medico_id = dto.MedicoId,
            especialidad_id = dto.EspecialidadId
        };

        if (currentUserRole == RolUsuario.Secretaria)
            turno.secretaria_id = currentUserId;

        _context.turnos.Add(turno);
        _stateService.Reservar(turno);


        slot.disponible = false;



        await _context.SaveChangesAsync(); //Para que se le asigne un valor al id de turno, no puedo hacerlo todo en un solo saveChanges ya que el turnoId no esta fijado como FK en la tabla auditoria

        var pacInfo = await _context.pacientes.Where(p => p.id == turno.paciente_id).Include(p => p.idNavigation).Select(p => new { p.idNavigation.nombre, p.dni,p.id }).FirstOrDefaultAsync();
        var medNombre = await _context.medicos.Where(m => m.id == turno.medico_id).Include(m => m.idNavigation).Select(m => new { m.idNavigation.nombre }).FirstOrDefaultAsync();
        var espNombre = await _context.especialidades.Where(e => e.id == turno.especialidad_id).Select(e => new { e.nombre }).FirstOrDefaultAsync();


        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id, //Es nulo porque no fue guardado con SaveChangesAsync
            UsuarioNombre = currentUserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.INSERT,
            FechaNueva = turno.fecha,
            HoraNueva = turno.hora,
            EstadoNuevo = _stateService.GetEstadoActual(turno).GetNombreEstado().ToEstadoTurno(),
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = pacInfo.nombre,
            PacienteDNI = pacInfo.dni,
            MedicoId = turno.medico_id.Value,
            MedicoNombre = medNombre.nombre,
            EspecialidadId = turno.especialidad_id.Value,
            EspecialidadNombre = espNombre.nombre,
            SlotIdNuevo = turno.slot_id,
        });

        _context.trazabilidadTurnos.Add(new TrazabilidadTurno
        {
            TurnoId = turno.id,
            UsuarioId = currentUserId,
            UsuarioRol = currentUserRole,
            UsuarioNombre = currentUserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.INSERT,
            Descripcion = currentUserRole == RolUsuario.Secretaria ? $"La secretaria {currentUserName} reservo un turno" : $"El paciente {currentUserName} reservo un turno"
        });

        await _context.SaveChangesAsync();


        return (true, "Turno creado exitosamente");
    }
    public async Task FinalizarAusentarTurnosPasados()
    {
        // Use the fully-qualified model type to avoid the Turno namespace/type conflict
        // and compare only by date here (time comparison omitted because the property was missing in the original expression).
       
        List<Turno> turnosPasados =await  _context.turnos.Include(t => t.slot). Include(t => t.medico).ThenInclude(m => m.idNavigation)
                                                  .Include(t => t.paciente).ThenInclude(p => p.idNavigation)
                                                  .Include(t => t.especialidad)
                                                  .Include(t => t.EntradaClinica)
                                                  .Where(t => t.slot != null && t.slot.disponible == false && t.fecha.Value.ToDateTime(t.slot.horafin.Value).AddMinutes(30) < DateTime.Now
                                                   && (t.estado == "Reservado"|| t.estado == "Reprogramado"))
                                                  .ToListAsync();
            
        List<Turno> turnosAAusentar = turnosPasados.Where(t => t.EntradaClinica == null).ToList();
        List<Turno> turnosAFinalizar = turnosPasados.Where(t => t.EntradaClinica != null).ToList();


        foreach(Turno turno in turnosAFinalizar)
        {
            RegistrarCambioEstadoTerminal(turno, AccionesTurno.FINALIZE,EstadosTurno.Finalizado,"El turno ha finalizado", RolUsuario.System.ToString());
            //_dispoService.LiberarSlot(turno.slot_id.Value);
            turno.slot_id = null;
            _stateService.Finalizar(turno);
        }

        foreach(Turno turno in turnosAAusentar)
        {
            RegistrarCambioEstadoTerminal(turno, AccionesTurno.NOSHOW,EstadosTurno.Ausentado,"El paciente no atendio al turno", RolUsuario.System.ToString());
            //_dispoService.LiberarSlot(turno.slot_id.Value);
            turno.slot_id = null;
            _stateService.MarcarAusente(turno);
        }

        await _context.SaveChangesAsync();

    }

    public async Task<(bool success, string message, TurnoEditDTO? turnoDto)> ReprogramarGet(int id)
    {
         // Busca el turno, incluyendo la información de su especialidad y paciente
            var turno = await _context.turnos
                               .Include(t => t.especialidad)
                               .Include(t => t.paciente)
                                   .ThenInclude(p => p!.idNavigation)
                               .FirstOrDefaultAsync(t => t.id == id);


            if (turno == null) return (false, "Turno no encontrado", null);

            var turnoState = _stateService.GetEstadoActual(turno);
            var result = await PuedeReprogramarReglas(turno);

            if (result.success)
            {
                // Crea el DTO y lo llenamos con los datos del turno
                var turnoDto = new TurnoEditDTO
                {
                    Id = turno.id,
                    Fecha = turno.fecha,
                    Hora = turno.hora,
                    PacienteId = turno.paciente_id,
                    PacienteNombre = turno.paciente?.idNavigation?.nombre,
                    MedicoId = turno.medico_id,
                    EspecialidadId = turno.especialidad_id ?? 0,
                    EspecialidadNombre = turno.especialidad?.nombre,
                    Estado = turno.estado!.ToEstadoTurno(),
                    DescripcionEstado = _stateService.GetEstadoActual(turno).GetDescripcion()
                };
                return(true, "DTO creado", turnoDto);
            }

            return (false, result.message, null);
            
    }

    public async Task<(bool success, string message)> ReprogramarPost(int turno_id, int nuevoSlotId)
    {
        var currentUserId = GetCurrentUserId()!.Value;
        var currentUserName = GetCurrentUserName()!;
        var currentUserRole = GetCurrentUserRole()!.Value;

        var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == turno_id);
        var nuevoSlot = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == nuevoSlotId);

        if (!await _dispoService.SlotEstaDisponible(nuevoSlot!.id))
            return (false,"El horario ya no esta disponible, recuerde que no puede asignar un horario para el dia actual");

        if (!_stateService.PuedeReprogramar(turno!))
            return (false, "El turno ya no puede reprogramarse" );

        if(turno.fecha.Value.ToDateTime(turno.hora.Value) <= DateTime.Now.AddHours(24))
           return (false, "El turno ya no puede reprogramarse, faltan menos de 24 hs para el mismo");

        await _context.turnos.Entry(turno).Reference(t => t.paciente).Query().Include(p => p.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.medico).Query().Include(m => m.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.especialidad).LoadAsync();

        //var slotViejo = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == turno.slot_id);

        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = currentUserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.UPDATE,
            FechaNueva = nuevoSlot.fecha,
            FechaAnterior = turno.fecha,
            HoraNueva = nuevoSlot.horainicio,
            HoraAnterior = turno.hora,
            EstadoNuevo = EstadosTurno.Reprogramado,
            EstadoAnterior = _stateService.GetEstadoActual(turno).GetNombreEstado().ToEstadoTurno(),
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
            PacienteDNI = turno.paciente.dni,
            MedicoId = turno.medico_id.Value,
            MedicoNombre = turno.medico.idNavigation.nombre,
            EspecialidadId = turno.especialidad_id.Value,
            EspecialidadNombre = turno.especialidad.nombre,
            SlotIdNuevo = nuevoSlotId,
            SlotIdAnterior = turno.slot_id
        });

        _context.trazabilidadTurnos.Add(new TrazabilidadTurno
        {
            TurnoId = turno.id,
            UsuarioId = currentUserId,
            UsuarioNombre = currentUserName,
            UsuarioRol = currentUserRole,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.UPDATE,
            Descripcion = currentUserRole == RolUsuario.Secretaria ? $"La secretaria {currentUserName} reprogramo un turno" : $"El paciente {currentUserName} reprogramo un turno"
        });

        // Guardar el ID del slot viejo ANTES de cambiarlo
        var slotViejoId = turno.slot_id.Value;

        turno.slot_id = nuevoSlotId;
        turno.slot = nuevoSlot;  //Tengo que ponerlo, sino como EF prioriza las propiedades de navegacion ve lo de abajo y piensa que el turno no debe apuntar a ningun slot
        turno.fecha = nuevoSlot.fecha;
        turno.hora = nuevoSlot.horainicio;
        turno.medico_id = nuevoSlot.medico_id;
        _stateService.Reprogramar(turno);

        // Marcar el nuevo slot como no disponible
        nuevoSlot.disponible = false;

        // Liberar el slot viejo (usando el ID guardado)
        var slotViejo = new SlotAgenda { id = slotViejoId, disponible = true};
        _context.Attach(slotViejo);
        _context.Entry(slotViejo).Property(sa => sa.disponible).IsModified = true;
        //_context.Entry(slotViejo).Reference(sa => sa.Turno).CurrentValue = null; ya se rompe la relacion cambiando la Fk y prop de nav desde la otra entidad
       // _context.Entry(slotViejo).Reference(sa => sa.Turno).IsModified = true;

        await _context.SaveChangesAsync();

        
        return (true, "El turno fue reprogramado exitosamente" );
        
        
    }

    public async Task<bool> CancelarPost(TurnoCancelDTO dto)
    {
        var currentUserId = GetCurrentUserId()!.Value;
        var currentUserName = GetCurrentUserName()!;
        var currentUserRole = GetCurrentUserRole()!.Value;

        var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == dto.Id);

        if (turno == null) return false;

        
        //Vale la pena cargar las propiedades de navegacion aca en vez de en la consulta original porque puede ser que en ese momento no fueran necesarias (si no se puede cancelar) y las estariamos cargando sin proposito alguno
        await _context.turnos.Entry(turno).Reference(t => t.medico).Query().Include(m => m.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.paciente).Query().Include(p => p.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.especialidad).LoadAsync();
        //await _context.turnos.Entry(turno).Reference(t => t.slot).LoadAsync();
        //int? slotViejoId =  _context.turnos.Entry(turno).Property(t=>t.slot_id).CurrentValue;


        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = currentUserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.CANCEL,
            FechaAnterior = turno.fecha,
            FechaNueva = turno.fecha,
            HoraAnterior = turno.hora,
            HoraNueva = turno.hora,
            EstadoAnterior = _stateService.GetEstadoActual(turno).GetNombreEstado().ToEstadoTurno(),
            EstadoNuevo = EstadosTurno.Cancelado,
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
            PacienteDNI = turno.paciente.dni,
            MedicoId = turno.medico_id.Value,
            MedicoNombre = turno.medico.idNavigation.nombre,
            EspecialidadId = turno.especialidad_id.Value,
            EspecialidadNombre = turno.especialidad.nombre,
            SlotIdAnterior = turno.slot_id,
            SlotIdNuevo = null,
            MotivoCancelacion = dto.MotivoCancelacion
        });

        _context.trazabilidadTurnos.Add(new TrazabilidadTurno
        {
            TurnoId = turno.id,
            UsuarioId = currentUserId,
            UsuarioRol = currentUserRole,
            UsuarioNombre = currentUserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.CANCEL,
            Descripcion = currentUserRole == RolUsuario.Secretaria ? $"La secretaria {currentUserName} cancelo un turno" : $"El paciente {currentUserName} cancelo un turno"
        });

        _stateService.Cancelar(turno, dto.MotivoCancelacion);

        var slotUpdate = new SlotAgenda { id = turno.slot_id.Value, disponible = true };
        turno.slot_id = null;
        _context.Attach(slotUpdate);


        _context.Entry(slotUpdate).Property(sa => sa.disponible).IsModified = true;


        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TurnoSvReprogamarResult> PuedeReprogramarReglas(Turno turno)
    {
        var currentUserRole = GetCurrentUserRole()!.Value;

        if (turno == null)
        {
            return new TurnoSvReprogamarResult{success = false, message = "El turno no fue encontrado"};
        }

        if (!_stateService.PuedeReprogramar(turno))
        {
            return new TurnoSvReprogamarResult {success = false, message = $"Este turno no puede ser reprogramado. Estado actual: {_stateService.GetEstadoActual(turno).GetNombreEstado()}"};
        }

        
        if(currentUserRole != RolUsuario.Secretaria)
        {
            if(turno.fecha.Value.ToDateTime(turno.hora.Value) < DateTime.Now.AddHours(24))
            {
                return new TurnoSvReprogamarResult {success = false, message = "El turno solo puede ser Reprogramado con menos de 24 hs de antelacion por una secretaria"};
            }
        }

        await _context.Entry(turno).Reference(t => t.medico).LoadAsync();
        await _context.Entry(turno.medico).Reference(m => m.idNavigation).LoadAsync();
        await _context.Entry(turno).Reference(t => t.paciente).LoadAsync();
        await _context.Entry(turno.paciente).Reference(p=> p.idNavigation).LoadAsync();

        return new TurnoSvReprogamarResult 
        {
            success = true, 
            message = "El turno puede reprogramarse", 
            fecha = turno.fecha.Value,
            hora = turno.hora.Value,
            medicoNombre = turno.medico.idNavigation.nombre,
            pacienteNombre = turno.paciente.idNavigation.nombre
        };
    }
    public async Task<TurnoSvCancelResult> PuedeCancelarReglas(int id)
    {
        var currentUserRole = GetCurrentUserRole()!.Value;

        var turno = await _context.turnos
                        .Where(t => t.id == id)
                        .FirstOrDefaultAsync();

        if (turno == null)
        {
            return new TurnoSvCancelResult{success = false, message = "El turno no fue encontrado"};
        }

        if (!_stateService.PuedeCancelar(turno))
        {
            return new TurnoSvCancelResult {success = false, message = $"Este turno no puede ser cancelado. Estado actual: {_stateService.GetEstadoActual(turno).GetNombreEstado()}"};
        }

        if(currentUserRole != RolUsuario.Secretaria)
        {
            if(turno.fecha.Value.ToDateTime(turno.hora.Value) < DateTime.Now.AddHours(24))
            {
                return new TurnoSvCancelResult {success = false, message = "El turno solo puede ser cancelado con menos de 24 hs de antelacion por una secretaria"};
            }
        }

        await _context.Entry(turno).Reference(t => t.medico).LoadAsync();
        await _context.Entry(turno.medico).Reference(m => m.idNavigation).LoadAsync();
        await _context.Entry(turno).Reference(t => t.paciente).LoadAsync();
        await _context.Entry(turno.paciente).Reference(p=> p.idNavigation).LoadAsync();

        return new TurnoSvCancelResult 
        {
            success = true, 
            message = "El turno puede cancelarse", 
            fecha = turno.fecha.Value,
            hora = turno.hora.Value,
            medicoNombre = turno.medico.idNavigation.nombre,
            pacienteNombre = turno.paciente.idNavigation.nombre
        };
    }

    public async Task<List<TurnoViewDTO>> ObtenerTurnosADividir()
    {
        var currentUserRole = GetCurrentUserRole()!.Value;
        var currentUserId = GetCurrentUserId()!.Value;

        IQueryable<Turno> query = _context.turnos.Include(t => t.medico).ThenInclude(m => m.idNavigation)
                                    .Include(t => t.especialidad);

        if(currentUserRole == RolUsuario.Paciente)
        {
            query = query.Where(t => t.paciente_id == currentUserId);
        }
        query = query.Include(t => t.paciente).ThenInclude(p => p.idNavigation);
                
        List<Turno> turnos = await query.ToListAsync();

        var turnosGestionables = new List<TurnoViewDTO>();

        foreach (var t in turnos)
        {
            var cancelResult = await PuedeCancelarReglas(t.id);
            var reprogramResult = await PuedeReprogramarReglas(t);

                TurnoViewDTO turnoDTO = new TurnoViewDTO
                {
                    Id = t.id,
                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                    Estado = _stateService.GetEstadoActual(t).GetNombreEstado(),
                    MedicoNombre = t.medico.idNavigation.nombre,
                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                    PuedeCancelar = cancelResult.success,
                    PuedeReprogramar = reprogramResult.success,
                    EsProximo = t.fecha.Value.ToDateTime(t.hora.Value) <= DateTime.Now.AddHours(24)
                                && t.fecha.Value.ToDateTime(t.hora.Value) > DateTime.Now
                };
            if(currentUserRole == RolUsuario.Secretaria)
            {
                turnoDTO.PacienteNombre = t.paciente.idNavigation.nombre;
            }
            
            turnosGestionables.Add(turnoDTO);
        }

        return turnosGestionables;
    }

    private void RegistrarCambioEstadoTerminal(Turno turno, AccionesTurno accion, EstadosTurno estadoNuevo, string descripcion, string usuarioNombre)
    {
        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = usuarioNombre,
            MomentoAccion = DateTime.UtcNow,
            Accion = accion,
            FechaAnterior = turno.fecha,
            HoraAnterior = turno.hora,
            EstadoAnterior = _stateService.GetEstadoActual(turno).GetNombreEstado().ToEstadoTurno(),
            EstadoNuevo = estadoNuevo,
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
            PacienteDNI = turno.paciente.dni,
            MedicoNombre = turno.medico.idNavigation.nombre,
            EspecialidadId = turno.especialidad_id.Value,
            EspecialidadNombre = turno.especialidad.nombre,
            SlotIdAnterior = turno.slot_id
        });

        _context.trazabilidadTurnos.Add(new TrazabilidadTurno
        {
            TurnoId = turno.id,
            UsuarioNombre = usuarioNombre,
            MomentoAccion = DateTime.UtcNow,
            Accion = accion,
            Descripcion = descripcion,
            UsuarioRol = RolUsuario.System
        });
    }

    public async Task<Turno> GetTurnoActual(int paciente_id, int medico_id)
    {
        Turno turnoActual = await _context.turnos.Where(t => t.paciente_id == paciente_id && t.medico_id == medico_id && t.fecha.Value.ToDateTime(t.slot.horainicio.Value).AddMinutes(-5) < DateTime.Now  && DateTime.Now < t.fecha.Value.ToDateTime(t.slot.horafin.Value).AddMinutes(30)).FirstOrDefaultAsync();//sirve porque los turnos no pueden ser asignados de madrugada

        if(turnoActual == null) return null;
        
        return turnoActual;
    }
     
}