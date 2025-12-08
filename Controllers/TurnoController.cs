using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.Models;
using MedCenter.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedCenter.Services.TurnoStates;
using MedCenter.Services.TurnoSv;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.Security.Claims;
using AspNetCoreGeneratedDocument;
using System.Net.WebSockets;
using MedCenter.Controllers;
using MedCenter.Services.DisponibilidadMedico;
using MedCenter.Services.EspecialidadService;
using System.Reflection.Metadata.Ecma335;
using Microsoft.VisualBasic;
using MedCenter.Extensions;



// Heredamos de Controller para poder trabajar con Vistas Razor
public class TurnoController : BaseController
{
    private readonly AppDbContext _context;
    private TurnoStateService _stateService;
    private DisponibilidadService _disponibilidadService;
    private EspecialidadService _especialidadService;
    private readonly TurnoService _turnoService;
    public TurnoController(AppDbContext context, TurnoStateService turnoStateService, DisponibilidadService disponibilidadService, EspecialidadService especialidadService, TurnoService turnoService)
    {
        _context = context;
        _stateService = turnoStateService;
        _disponibilidadService = disponibilidadService;
        _especialidadService = especialidadService;
        _turnoService = turnoService;
    }

   

    // GET: Turno
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var turnos = await _context.turnos.Where(t => t.estado == EstadosTurno.Reservado.ToString() || t.estado == EstadosTurno.Reprogramado.ToString())
            .Select(t => new TurnoViewDTO
            {
                Id = t.id,
                Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha",
                Hora = t.hora.HasValue ? t.hora.Value.ToString(@"hh\:mm") : "Sin hora",
                Estado = t.estado,
                PacienteNombre = (t.paciente != null && t.paciente.idNavigation != null) ? t.paciente.idNavigation.nombre : "Desconocido",
                MedicoNombre = (t.medico != null && t.medico.idNavigation != null)
                                ? t.medico.idNavigation.nombre
                                : "Desconocido"
            })
            .ToListAsync();

        // Devolvemos la vista "Index.cshtml" y le pasamos la lista de DTOs como modelo.
        // Esto conecta directamente con el @model IEnumerable<TurnoViewDTO> de tu archivo .cshtml
        return View(turnos);
    }



    // GET: Turno/GetEspecialidadesPorMedico/5
    [HttpGet] //quizas borrar ya que ahora se usa el inverso, osea obtener medicos por la especialidad elegida, mejor para el dominio del problema
    public async Task<JsonResult> GetEspecialidadesPorMedico(int medicoId)
    {
        var especialidades = await _context.medicoEspecialidades
                                           .Where(me => me.medicoId == medicoId && me.especialidad != null)
                                           .Include(me => me.especialidad) // Incluimos los datos de la especialidad
                                           .Select(me => new { id = me.especialidad.id, nombre = me.especialidad.nombre })
                                           .ToListAsync();

        return Json(especialidades);
    }

    public async Task<JsonResult> GetMedicosPorEspecialidad(int especialidadId)
    {
        var medicos = await _especialidadService.GetMedicosPorEspecialidad(especialidadId);

        return Json(medicos);
    }
    
    public async Task<JsonResult> ObtenerSlotsDelDia(int medicoId, DateOnly fecha)
    {
        List<SlotAgenda> slots = await _disponibilidadService.GetSlotsDisponibles(medicoId,fecha);
        return Json(slots);
    }

    public async Task<IActionResult> Reservar()
    {
        var especialidades = await _especialidadService.GetEspecialidadesCargadas();
        ViewBag.UserName = UserName;
        ViewBag.EsSecretaria = User.IsInRole(RolUsuario.Secretaria.ToString());

        if (User.IsInRole(RolUsuario.Secretaria.ToString()))
        {
            ViewBag.Pacientes = await _context.pacientes.Include(p => p.idNavigation)
                                                        .Select(p => new { p.idNavigation.nombre, p.dni, p.id })
                                                        .OrderBy(p => p.nombre)
                                                        .ToListAsync();
        }
        return View("~/Views/Shared/Turnos/SolicitarTurno.cshtml",especialidades);
    }

    [HttpPost]
    public async Task<IActionResult> Reservar(ReservarTurnoDTO dto, int? pacienteElegidoId)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _turnoService.Reservar(dto, pacienteElegidoId);

        return Json(new{success = result.success , message = result.message });

        
       /* if (!await _disponibilidadService.SlotEstaDisponible(dto.SlotId))
            return Json(new { success = false, message = "El horario ya no esta disponible" }); //Los Json se crean de forma dinamica

        var slot = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == dto.SlotId);

        if (slot == null)
            return Json(new { success = false, message = "No se encontro el horario" });

        if(slot.fecha <= DateOnly.FromDateTime(DateTime.Now)) 
            return Json(new {succes = false, message = "No pueden reservarse turnos para el dia actual o hacia el pasado, hagalo con mas antelacion"});
        
        int pacienteFinalId;

        if (UserRole == RolUsuario.Secretaria)
        {
            if (!pacienteElegidoId.HasValue)
            {
                return Json(new { success = false, message = "Debe elegir un paciente" });
            }
            pacienteFinalId = pacienteElegidoId.Value;
        }
        else
        {
            pacienteFinalId = UserId.Value;
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

        if (UserRole == RolUsuario.Secretaria)
            turno.secretaria_id = UserId;

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
            UsuarioNombre = UserName,
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
            UsuarioId = UserId.Value,
            UsuarioRol = UserRole.Value,
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Accion = AccionesTurno.INSERT,
            Descripcion = User.IsInRole("Secretaria") ? $"La secretaria {UserName} reservo un turno" : $"El paciente {UserName} reservo un turno"
        });

        await _context.SaveChangesAsync();


        return Json(new { success = true, message = "Turno creado exitosamente" });*/
    }
    
    public async Task<IActionResult> Reprogramar(int? id)
    {
        if (id == null) return NotFound();
        
            /* Busca el turno, incluyendo la información de su especialidad y paciente
            var turno = await _context.turnos
                               .Include(t => t.especialidad)
                               .Include(t => t.paciente)
                                   .ThenInclude(p => p!.idNavigation)
                               .FirstOrDefaultAsync(t => t.id == id);


            if (turno == null) return NotFound();

            var turnoState = _stateService.GetEstadoActual(turno);
            var result = await PuedeReprogramarReglas(turno);
            */
            var result = await _turnoService.ReprogramarGet(id.Value);

            if (result.success)
            {
                //Filtra los médicos por la especialidad del turno
                ViewData["MedicoId"] = _especialidadService.GetMedicosPorEspecialidad(result.turnoDto!.EspecialidadId);
                // Información del estado actual
                ViewBag.EstadoActual = result.turnoDto.Estado.ToString();
                ViewBag.DescripcionEstado = result.turnoDto.DescripcionEstado;
                ViewBag.UserName = UserName;

                return View("~/Views/Shared/Turnos/ReprogramarTurno.cshtml",result.turnoDto);
            }

            TempData["ErrorMessage"] = result.message;
            return RedirectToAction(nameof(Index));
        

    }


    [HttpPost]
    public async Task<IActionResult> Reprogramar(int turno_id, int nuevoSlotId)
    {
        var result = await _turnoService.ReprogramarPost(turno_id,nuevoSlotId);

        return Json(new{success = result.success, message = result.message});
        
    }
    public async Task<JsonResult> GetDiasDisponibles(int medicoId)
    {
        var dias = await _disponibilidadService.GetDiasDisponibles(medicoId);

        return Json(dias.Select(d => d.ToString("yyyy-MM-dd")));
    }

    [HttpGet]
    public async Task<IActionResult> GetDiasConDisponibilidad(int medicoId)
    {
        try
        {
            var diasDisponibles = await _disponibilidadService.GetDiasDisponibles(medicoId);
            var disponibilidadPorDia = await _disponibilidadService.GetColoresSemaforo(medicoId);

            var resultado = diasDisponibles.Select(fecha => new
            {
                fecha = fecha.ToString("yyyy-MM-dd"),
                color = disponibilidadPorDia.ContainsKey(fecha) 
                    ? disponibilidadPorDia[fecha].ToString().ToLower() 
                    : "verde"
            }).ToList();

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Error al obtener disponibilidad por día");
            return StatusCode(500, "Error al cargar la disponibilidad");
        }
    }

    public async Task<JsonResult> GetSlotsDisponibles(int medicoId, DateOnly fecha)
    {
        var slots = await _disponibilidadService.GetSlotsDisponibles(medicoId, fecha);
        var dtos = slots.Select(sa => new SlotAgendaViewDTO
        {
            Id = sa.id,
            HoraInicio = sa.horainicio,
            HoraFin = sa.horafin,
            Disponible = sa.disponible ?? false
        });
        return Json(dtos);
    }

    public async Task<JsonResult> GetTodosLosSlots(int medicoId, DateOnly fecha)
    {
        var slots = await _disponibilidadService.GetTodosLosSlots(medicoId, fecha);
        var dtos = slots.Select(sa => new SlotAgendaViewDTO
        {
            Id = sa.id,
            HoraInicio = sa.horainicio,
            HoraFin = sa.horafin,
            Disponible = sa.disponible ?? false
        });
        return Json(dtos);
    }

    public async Task<IActionResult> Cancelar(int id)
    {
        
        ViewBag.UserName = UserName;

        TurnoSvCancelResult result = await _turnoService.PuedeCancelarReglas(id);

        
            if (!result.success)
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction("Dashboard", UserRole.Value.ToString());
            }

            var view = User.IsInRole("Paciente") ? "~/Views/Paciente/ConfirmarCancelacion.cshtml"
                                                       : "~/Views/Secretaria/ConfirmarCancelacion.cshtml";

            var turnoDto = new TurnoCancelDTO // <-- Usamos el nuevo DTO
            {
                Id = id,
                Fecha = result.fecha.ToString(),
                Hora = result.hora.ToString(),
                PacienteNombre = result.pacienteNombre,
                MedicoNombre = result.medicoNombre
                // La propiedad MotivoCancelacion se deja , la llena el usuario
            };

            return View(view, turnoDto);
        
    }
    
    public async Task<IActionResult> GestionarTurnos()
    {
        ViewBag.UserName = UserName;

        await _turnoService.FinalizarAusentarTurnosPasados();
        await _disponibilidadService.LimpiarSlotsPasados();
        
        var turnos = await _turnoService.ObtenerTurnosADividir();

        var view = User.IsInRole("Paciente") ? "~/Views/Paciente/GestionarTurnos.cshtml" : "~/Views/Secretaria/GestionarTurnos.cshtml";

        return View(view, turnos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(TurnoCancelDTO dto)
    {
        if (!ModelState.IsValid)
        {

            if (User.IsInRole("Paciente"))
                return View("~/Views/Paciente/ConfirmarCancelacion.cshtml", dto);

            return View("~/Views/Secretaria/ConfirmarCancelacion.cshtml", dto);
        }

        bool result = await _turnoService.CancelarPost(dto);
        if(!result)
        return NotFound();

        if (User.IsInRole("Paciente"))
            return RedirectToAction($"Dashboard", "Paciente");
        return RedirectToAction($"Dashboard", "Secretaria");

    }

    private async Task<bool> TurnoExists(int id)
    {
        return await _context.turnos.AnyAsync(t => t.id == id);
    } 

}