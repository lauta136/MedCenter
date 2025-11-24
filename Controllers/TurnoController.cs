using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.Models;
using MedCenter.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedCenter.Services.TurnoStates;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.Security.Claims;
using AspNetCoreGeneratedDocument;
using System.Net.WebSockets;
using MedCenter.Controllers;
using MedCenter.Services.DisponibilidadMedico;
using MedCenter.Services.EspecialidadService;
using MedCenter.Services.TurnoSv;


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

    // GET: Turno/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var turnoDto = await _context.turnos
            .Where(t => t.id == id)
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
            .FirstOrDefaultAsync();

        if (turnoDto == null)
        {
            return NotFound();
        }

        // Devolvemos la vista "Details.cshtml" y le pasamos el DTO como modelo
        return View(turnoDto);
    }

    // GET: Turno
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var turnos = await _context.turnos.Where(t => t.estado == "Reservado" || t.estado == "Reprogramado")
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

    // GET: Turno/Create
    public async Task<IActionResult> Create()
    {
        // 1. Lista de pacientes para el dropdown
        var pacientes = await _context.personas
                                  .Where(p => _context.pacientes.Any(pa => pa.id == p.id))
                                  .ToListAsync();
        ViewData["PacienteId"] = new SelectList(pacientes, "id", "nombre");

        // 2. Lista de médicos para el dropdown
        var medicos = await _context.personas
                                .Where(p => _context.medicos.Any(m => m.id == p.id))
                                .ToListAsync();
        ViewData["MedicoId"] = new SelectList(medicos, "id", "nombre");

        return View();
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

    

    // POST: Turno/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("fecha,hora,paciente_id,medico_id,especialidad_id")] Turno turno)
    {

        if (ModelState.IsValid) //Agregar el chequeo de si SlotAgenda de esa fecha esta libre para ese medico
        {
            try
            {
                turno.estado = "Disponible";
                _context.Add(turno);
                await _context.SaveChangesAsync();
                _stateService.Reservar(turno);
                _context.Update(turno);
                await _context.SaveChangesAsync();

                TempData["SuccessMesage"] = "Turno creado y reservado exitosamente";

                return RedirectToAction(nameof(Index));
            }
            catch (TransicionDeEstadoInvalidaException e)
            {
                ModelState.AddModelError("", e.Message); //mensaje es luego accedido por la vista mediante @Html.ValidationSummary() o @Html.ValidationMessageFor() y mostrado
            }
        }

        // Si el modelo no es válido, volvemos a cargar las listas iniciales
        ViewData["PacienteId"] = new SelectList(await _context.personas.Where(p => _context.pacientes.Any(pa => pa.id == p.id)).ToListAsync(), "id", "nombre", turno.paciente_id);
        ViewData["MedicoId"] = new SelectList(await _context.personas.Where(p => _context.medicos.Any(m => m.id == p.id)).ToListAsync(), "id", "nombre", turno.medico_id);

        return View(turno);
    }

    public async Task<IActionResult> Reservar()
    {
        var especialidades = await _especialidadService.GetEspecialidadesCargadas();
        ViewBag.UserName = UserName;
        ViewBag.EsSecretaria = User.IsInRole("Secretaria");

        if (User.IsInRole("Secretaria"))
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

        if (!await _disponibilidadService.SlotEstaDisponible(dto.SlotId))
            return Json(new { success = false, message = "El horario ya no esta disponible" }); //Los Json se crean de forma dinamica

        var slot = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == dto.SlotId);

        if (slot == null)
            return Json(new { success = false, message = "No se encontro el horario" });

        if(slot.fecha <= DateOnly.FromDateTime(DateTime.Now)) 
            return Json(new {succes = false, message = "No pueden reservarse turnos para el dia actual, hagalo con mas antelacion"});

        int pacienteFinalId;

        if (UserRole == "Secretaria")
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

        if (UserRole == "Secretaria")
            turno.secretaria_id = UserId;

        _context.turnos.Add(turno);
        _stateService.Reservar(turno);


        slot.disponible = false;



        await _context.SaveChangesAsync(); //Para que se le asigne un valor al id de turno, no puedo hacerlo todo en un solo saveChanges ya que el turnoId no esta fijado como FK en la tabla auditoria

        //Cargo las propiedades de navegacion despues del SaveChangesAsync() para que se hayan rellenado con ef 
        /*await _context.turnos.Entry(turno).Reference(t => t.paciente).Query().Include(p => p.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.medico).Query().Include(m => m.idNavigation).LoadAsync();

        await _context.turnos.Entry(turno).Reference(t => t.especialidad).LoadAsync();
        */
        var pacNombre = await _context.pacientes.Where(p => p.id == turno.paciente_id).Include(p => p.idNavigation).Select(p => new { p.idNavigation.nombre }).FirstOrDefaultAsync();
        var medNombre = await _context.medicos.Where(m => m.id == turno.medico_id).Include(m => m.idNavigation).Select(m => new { m.idNavigation.nombre }).FirstOrDefaultAsync();
        var espNombre = await _context.especialidades.Where(e => e.id == turno.especialidad_id).Select(e => new { e.nombre }).FirstOrDefaultAsync();


        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id, //Es nulo porque no fue guardado con SaveChangesAsync
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.UtcNow,
            Accion = "INSERT",
            FechaNueva = turno.fecha,
            HoraNueva = turno.hora,
            EstadoNuevo = turno.estado,
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = pacNombre.nombre,
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
            UsuarioRol = UserRole,
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.UtcNow,
            Accion = "INSERT",
            Descripcion = User.IsInRole("Secretaria") ? $"La secretaria {UserName} reservo un turno" : $"El paciente {UserName} reservo un turno"
        });

        await _context.SaveChangesAsync();


        return Json(new { success = true, message = "Turno creado exitosamente" });
    }
    
    public async Task<IActionResult> Reprogramar(int? id)
    {
        if (id == null) return NotFound();
        

            // Busca el turno, incluyendo la información de su especialidad y paciente
            var turno = await _context.turnos
                               .Include(t => t.especialidad)
                               .Include(t => t.paciente)
                                   .ThenInclude(p => p!.idNavigation)
                               .FirstOrDefaultAsync(t => t.id == id);


            if (turno == null) return NotFound();

            var turnoState = _stateService.GetEstadoActual(turno);

            if (turnoState.PuedeReprogramar(turno))
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
                    EspecialidadNombre = turno.especialidad?.nombre
                };


                //Filtra los médicos por la especialidad del turno
                ViewData["MedicoId"] = new SelectList( //Quizas vambiar por un Json para vista AJAX
                _context.medicos
                    .Where(m => m.medicoEspecialidades.Any(me => me.especialidadId == turno.especialidad_id))
                    .Select(m => m.idNavigation), "id", "nombre", turnoDto.MedicoId);

                // Información del estado actual
                var estadoActual = _stateService.GetEstadoActual(turno);
                ViewBag.EstadoActual = estadoActual.GetNombreEstado();
                ViewBag.DescripcionEstado = estadoActual.GetDescripcion();
                ViewBag.UserName = UserName;

                return View("~/Views/Shared/Turnos/ReprogramarTurno.cshtml",turnoDto);
            }

            TempData["ErrorMessage"] = "El turno no es reprogramable." +
            (turno.estado == "Reprogramado" ? "Ya fue reprogramado con anterioridad" : $"Estado actual:{turno.estado}");
            return RedirectToAction(nameof(Index));
        

    }


    [HttpPost]
    public async Task<IActionResult> Reprogramar(int turno_id, int nuevoSlotId)
    {
        var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == turno_id);
        var nuevoSlot = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == nuevoSlotId);

        if (!await _disponibilidadService.SlotEstaDisponible(nuevoSlot!.id))
            return Json(new { success = false, errormessage = "El horario ya no esta disponible, recuerde que no puede asignar un horario para el dia actual" });

        if (!_stateService.PuedeReprogramar(turno!))
            return Json(new { success = false, errormessage = "El turno ya no puede reprogramarse" });

        await _context.turnos.Entry(turno).Reference(t => t.paciente).Query().Include(p => p.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.medico).Query().Include(m => m.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.especialidad).LoadAsync();

        //var slotViejo = await _context.slotsagenda.FirstOrDefaultAsync(sa => sa.id == turno.slot_id);

        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.UtcNow,
            Accion = "UPDATE",
            FechaNueva = nuevoSlot.fecha,
            FechaAnterior = turno.fecha,
            HoraNueva = nuevoSlot.horainicio,
            HoraAnterior = turno.hora,
            EstadoNuevo = "Reprogramado",
            EstadoAnterior = _stateService.GetEstadoActual(turno).GetNombreEstado(),
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
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
            UsuarioId = UserId.Value,
            UsuarioNombre = UserName,
            UsuarioRol = UserRole,
            MomentoAccion = DateTime.UtcNow,
            Accion = "UPDATE",
            Descripcion = User.IsInRole("Secretaria") ? $"La secretaria {UserName} reprogramo un turno" : $"El paciente {UserName} reprogramo un turno"
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

        
        return Json(new { success = true, message = "El turno fue reprogramado exitosamente" });
        
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

    //Eliminar
    public async Task<IActionResult> Cancel(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var turno = await _context.turnos
            .Where(t => t.id == id)
            .Include(t => t.medico)
            .ThenInclude(m => m!.idNavigation)
            .Include(t => t.paciente)
            .ThenInclude(p =>p!.idNavigation)
            .FirstOrDefaultAsync();

        

        if (turno == null)
        {
            return NotFound();
        }
        
        if (!_stateService.PuedeCancelar(turno))
        {
            TempData["ErrorMessage"] = $"Este turno no puede ser cancelado. Estado actual: {turno.estado}";
            return RedirectToAction(nameof(Index));
        }

        var turnoDto = new TurnoCancelDTO // <-- Usamos el nuevo DTO
        {
            Id = turno.id,
            Fecha = turno.fecha.HasValue ? turno.fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha",
            Hora = turno.hora.HasValue ? turno.hora.Value.ToString(@"hh\:mm") : "Sin hora",
            PacienteNombre = (turno.paciente != null && turno.paciente.idNavigation != null) ? turno.paciente.idNavigation.nombre : "Desconocido",
            MedicoNombre = (turno.medico != null && turno.medico.idNavigation != null) ? turno.medico.idNavigation.nombre : "Desconocido"
            // La propiedad MotivoCancelacion se deja , la llena el usuario
        };
        
        // Devolvemos la vista de confirmación con el DTO lleno
        return View(turnoDto);
    }


    //Eliminar
    [HttpPost, ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(TurnoCancelDTO dto)
    {
        // Verificamos si el motivo (que es requerido en el DTO) fue ingresado.
        if (ModelState.IsValid)
        {

            try
            {
                // Buscamos el turno original desde la base de datos usando el Id del DTO
                var turno = await _context.turnos.FindAsync(dto.Id);

                if (turno != null)
                {
                    // Actualizamos ambos campos
                    _stateService.Cancelar(turno, dto.MotivoCancelacion);
                    //turno.motivo_cancelacion = dto.MotivoCancelacion;
                    _context.Update(turno);
                    await _context.SaveChangesAsync();
                }

                // Redirigimos a la lista de turnos
                return RedirectToAction(nameof(Index));
            }

            catch (TransicionDeEstadoInvalidaException e)
            {
                TempData["ErrorMessage"] = e.Message;
                return RedirectToAction(nameof(Index));
            }
        }


        // Si el ModelState no es válido (ej: el motivo está vacío),
        // volvemos a mostrar la misma vista de cancelación.
        // El DTO ya tiene el error de validación, por lo que el span mostrará el mensaje.
        // Sin embargo, los datos del turno (Fecha, Hora, etc.) se perdieron.
        // ¡Necesitamos recargarlos antes de volver a mostrar la vista!
        var turnoOriginal = await _context.turnos.FindAsync(dto.Id);
        if (turnoOriginal != null)
        {
            dto.Fecha = turnoOriginal.fecha?.ToString("yyyy-MM-dd");
            dto.Hora = turnoOriginal.hora?.ToString(@"hh\:mm");
            dto.PacienteNombre = (await _context.pacientes.Include(p => p.idNavigation).FirstOrDefaultAsync(p => p.id == turnoOriginal.paciente_id))?.idNavigation.nombre;
            dto.MedicoNombre = (await _context.medicos.Include(m => m.idNavigation).FirstOrDefaultAsync(m => m.id == turnoOriginal.medico_id))?.idNavigation.nombre;
        }

        return View("Cancel",dto);
    }

    // Eliminar
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        // Busca el turno, incluyendo la información de su especialidad y paciente
        var turno = await _context.turnos
                               .Include(t => t.especialidad)
                               .Include(t => t.paciente)
                                   .ThenInclude(p => p!.idNavigation)
                               .FirstOrDefaultAsync(t => t.id == id);


        if (turno == null) return NotFound();

        var turnoState = _stateService.GetEstadoActual(turno);

        if (turnoState.PuedeReprogramar(turno))
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
                EspecialidadNombre = turno.especialidad?.nombre
            };


            //Filtra los médicos por la especialidad del turno
            ViewData["MedicoId"] = new SelectList(
            _context.medicos
                    .Where(m => m.medicoEspecialidades.Any(me => me.especialidadId == turno.especialidad_id))
                    .Select(m => m.idNavigation), "id", "nombre", turnoDto.MedicoId);

            // Información del estado actual
            var estadoActual = _stateService.GetEstadoActual(turno);
            ViewBag.EstadoActual = estadoActual.GetNombreEstado();
            ViewBag.DescripcionEstado = estadoActual.GetDescripcion();

            return View(turnoDto);
        }

        TempData["ErrorMessage"] = "El turno no es reprogramable." +
        (turno.estado == "Reprogramado" ? "Ya fue reprogramado con anterioridad" : $"Estado actual:{turno.estado}");
        return RedirectToAction(nameof(Index));
    }


    // POST: Turno/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Fecha,Hora,PacienteId,MedicoId,EspecialidadId")] TurnoEditDTO turnoDto) //El Bind hace que solo se llene el turnoDto con las propiedades que especificas dentro de Bind, el resto queda vacio
    {
        if (id != turnoDto.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var turnoAActualizar = await _context.turnos.FindAsync(id);
                if (turnoAActualizar == null)
                {
                    return NotFound();
                }

                // Pasamos los valores del DTO al modelo original
                turnoAActualizar.fecha = turnoDto.Fecha;
                turnoAActualizar.hora = turnoDto.Hora;
                turnoAActualizar.paciente_id = turnoDto.PacienteId; // El ID viene del campo oculto
                turnoAActualizar.medico_id = turnoDto.MedicoId;

                _stateService.Reprogramar(turnoAActualizar);
                _context.Update(turnoAActualizar);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Turno reprogramado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (TransicionDeEstadoInvalidaException e)
            {
                TempData["ErrorMessage"] = e.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TurnoExists(id))
                    return NotFound();
                throw;
            }
        }

        // Si el modelo no es válido, recargamos solo la lista de médicos
        ViewData["MedicoId"] = new SelectList(
            _context.medicos.Where(m => m.medicoEspecialidades.Any(me => me.especialidadId == turnoDto.EspecialidadId)).Select(m => m.idNavigation),
            "id", "nombre",
            turnoDto.MedicoId);

        // También necesitamos recargar el nombre del paciente para mostrarlo de nuevo
        var persona = await _context.personas.FindAsync(turnoDto.PacienteId);
        turnoDto.PacienteNombre = persona?.nombre;

        return View(turnoDto);
    }

    public async Task<IActionResult> Cancelar(int id)
    {
        
        ViewBag.UserName = UserName;

       

        var turno = await ObtenerConfirmacionCancelacion(id);

        if (turno != null)
        {
            if (!_stateService.PuedeCancelar(turno))
            {
                    TempData["ErrorMessage"] = $"Este turno no puede ser cancelado. Estado actual: {turno.estado}";
                    return RedirectToAction(nameof(Index));
            }
            var view = User.IsInRole("Paciente") ? "~/Views/Paciente/ConfirmarCancelacion.cshtml"
                                                       : "~/Views/Secretaria/ConfirmarCancelacion.cshtml";

            var turnoDto = new TurnoCancelDTO // <-- Usamos el nuevo DTO
            {
                Id = turno.id,
                Fecha = turno.fecha.HasValue ? turno.fecha.Value.ToString() : "Sin fecha",
                Hora = turno.hora.HasValue ? turno.hora.Value.ToString() : "Sin hora",
                PacienteNombre = (turno.paciente != null && turno.paciente.idNavigation != null) ? turno.paciente.idNavigation.nombre : "Desconocido",
                MedicoNombre = (turno.medico != null && turno.medico.idNavigation != null) ? turno.medico.idNavigation.nombre : "Desconocido"
                // La propiedad MotivoCancelacion se deja , la llena el usuario
            };

            return View(view, turnoDto);
        }
        return NotFound();

       
    }
    
    public async Task<IActionResult> GestionarTurnos()
    {
        ViewBag.UserName = UserName;

        await _turnoService.FinalizarTurnosPasados();
        await _disponibilidadService.LimpiarSlotsPasados();
        
        var turnos = User.IsInRole("Paciente") ? await ObtenerTurnosGestionarPaciente() : await ObtenerTurnosGestionarSecretaria();

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

        var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == dto.Id);

        if (turno == null) return NotFound();

        if (!_stateService.PuedeCancelar(turno))
        {
            TempData["ErrorMessage"] = $"El turno no puede ser cancelado, Estado actual:{turno.estado}";

            // if (User.IsInRole("Paciente"))
            return RedirectToAction("Cancelar", "Turno"); //FIJATE SI ES ASI O DIRECTAMENTE DEVOLVER LA View de la seleccion a cancelar
        }

        
        //Vale la pena cargar las propiedades de navegacion aca en vez de en la consulta original porque puede ser que en ese momento no fueran necesarias (si no se puede cancelar) y las estariamos cargando sin proposito alguno
        await _context.turnos.Entry(turno).Reference(t => t.medico).Query().Include(m => m.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.paciente).Query().Include(p => p.idNavigation).LoadAsync();
        await _context.turnos.Entry(turno).Reference(t => t.especialidad).LoadAsync();
        //await _context.turnos.Entry(turno).Reference(t => t.slot).LoadAsync();
        //int? slotViejoId =  _context.turnos.Entry(turno).Property(t=>t.slot_id).CurrentValue;


        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.UtcNow,
            Accion = "CANCEL",
            FechaAnterior = turno.fecha,
            FechaNueva = turno.fecha,
            HoraAnterior = turno.hora,
            HoraNueva = turno.hora,
            EstadoAnterior = turno.estado,
            EstadoNuevo = "Cancelado",
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
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
            UsuarioId = UserId.Value,
            UsuarioRol = UserRole,
            UsuarioNombre = UserName,
            MomentoAccion = DateTime.UtcNow,
            Accion = "CANCEL",
            Descripcion = User.IsInRole("Secretaria") ? $"La secretaria {UserName} cancelo un turno" : $"El paciente {UserName} cancelo un turno"
        });

        _stateService.Cancelar(turno, dto.MotivoCancelacion);

        var slotUpdate = new SlotAgenda { id = turno.slot_id.Value, disponible = true };
        turno.slot_id = null;
        _context.Attach(slotUpdate);


        _context.Entry(slotUpdate).Property(sa => sa.disponible).IsModified = true;


        await _context.SaveChangesAsync();


        if (User.IsInRole("Paciente"))
            return RedirectToAction($"Dashboard", "Paciente");
        return RedirectToAction($"Dashboard", "Secretaria");

    }
    
    public async Task<Turno> ObtenerConfirmacionCancelacion(int id)
    {
        var turno = await _context.turnos
                        .Where(t => t.id == id)
                        .Include(t => t.medico)
                        .ThenInclude(m => m!.idNavigation)
                        .Include(t => t.paciente)
                        .ThenInclude(p => p!.idNavigation)
                        .FirstOrDefaultAsync();

        if (turno == null)
        {
            return null;
        }

        if (!_stateService.PuedeCancelar(turno))
        {
            TempData["ErrorMessage"] = $"Este turno no puede ser cancelado. Estado actual: {turno.estado}";
            //return RedirectToAction(nameof(Index));
        }


        return turno;
        // Devolvemos la vista de confirmación con el DTO lleno
        //return View("ConfirmarCancelacion", turnoDto);
    }

   /* public async Task<List<TurnoViewDTO>> ObtenerTurnosCancelarPaciente()
    {
        var turnos = await _context.turnos
                     .Where(t => t.paciente_id == UserId)
                     .Include(t => t.medico)
                     .ThenInclude(m => m.idNavigation)
                     .Include(t => t.especialidad)
                     .Where(t => t.fecha.Value.ToDateTime(t.hora.Value) > DateTime.Now.AddHours(24))
                     .ToListAsync();

        var turnosCancelables = turnos.Where(t => _stateService.PuedeCancelar(t) == true)
                                .Select(t => new TurnoViewDTO
                                {
                                    Id = t.id,
                                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                                    Estado = t.estado,
                                    MedicoNombre = t.medico.idNavigation.nombre,
                                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                                    PuedeCancelar = true,
                                    PuedeReprogramar = _stateService.PuedeReprogramar(t)
                                })
                                .ToList(); 

        return turnosCancelables;

    }
*/
   
    public async Task<List<TurnoViewDTO>> ObtenerTurnosGestionarPaciente()
    {

        var turnos = await _context.turnos
                     .Where(t => t.paciente_id == UserId)
                     .Include(t => t.medico)
                     .ThenInclude(m => m.idNavigation)
                     .Include(t => t.especialidad)
                     .Where(t => t.fecha.Value.ToDateTime(t.hora.Value) > DateTime.Now.AddHours(24))
                     .ToListAsync();

        var turnosGestionables = turnos.Where(t => _stateService.PuedeCancelar(t) || _stateService.PuedeReprogramar(t))
                                .Select(t => new TurnoViewDTO
                                {
                                    Id = t.id,
                                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                                    Estado = t.estado,
                                    MedicoNombre = t.medico.idNavigation.nombre,
                                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                                    PuedeCancelar = _stateService.PuedeCancelar(t),
                                    PuedeReprogramar = _stateService.PuedeReprogramar(t)
                                })
                                .ToList(); 

        return turnosGestionables;
    }

   /* public async Task<List<TurnoViewDTO>> ObtenerTurnosCancelarSecretaria()
    {
        var turnos = await _context.turnos
                     .Include(t => t.medico)
                     .ThenInclude(m => m.idNavigation)
                     .Include(t => t.paciente)
                     .ThenInclude(p => p.idNavigation)
                     .Include(t => t.especialidad)
                     .Where(t => t.fecha.Value.ToDateTime(t.hora.Value) > DateTime.Now.AddHours(24))
                    .ToListAsync();

        var turnosCancelables = turnos.Where(t => _stateService.PuedeCancelar(t) == true)
                                .Select(t => new TurnoViewDTO
                                {
                                    Id = t.id,
                                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                                    Estado = t.estado,
                                    MedicoNombre = t.medico.idNavigation.nombre,
                                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                                    PacienteNombre = t.paciente.idNavigation.nombre,
                                    PuedeCancelar = true,
                                    PuedeReprogramar = _stateService.PuedeReprogramar(t)
                                })
                                .ToList();
                                
        return turnosCancelables;
    }
*/
    public async Task<List<TurnoViewDTO>> ObtenerTurnosGestionarSecretaria()
    {
        var turnos = await _context.turnos
                     .Include(t => t.medico)
                     .ThenInclude(m => m.idNavigation)
                     .Include(t => t.paciente)
                     .ThenInclude(p => p.idNavigation)
                     .Include(t => t.especialidad)
                     .Where(t => t.fecha.Value.ToDateTime(t.hora.Value) >= DateTime.Now)
                     .ToListAsync();
 
        var turnosGestionables = turnos//Where(t => _stateService.PuedeCancelar(t) || _stateService.PuedeReprogramar(t))
                                .Select(t => new TurnoViewDTO
                                {
                                    Id = t.id,
                                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                                    Estado = t.estado,
                                    MedicoNombre = t.medico.idNavigation.nombre,
                                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                                    PacienteNombre = t.paciente.idNavigation.nombre,
                                    PuedeCancelar = _stateService.PuedeCancelar(t),
                                    PuedeReprogramar = _stateService.PuedeReprogramar(t)
                                })
                                .ToList();
                                
        return turnosGestionables;
    }
        
    [HttpPost]
    public async Task<IActionResult> Finalizar(int? id)
    {
       try
        {
            var turno = await _context.turnos.FindAsync(id);
            if (turno == null) return NotFound();

            // ⭐ Usar State Pattern para finalizar
            _stateService.Finalizar(turno);

            _context.Update(turno);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Turno finalizado exitosamente";
        }
        catch (TransicionDeEstadoInvalidaException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TurnoExists(int id)
    {
        return await _context.turnos.AnyAsync(t => t.id == id);
    } 

}