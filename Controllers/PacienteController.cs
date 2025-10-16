using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.DTOs;
using System.Security.Claims;
using MedCenter.Services.TurnoStates;

namespace MedCenter.Controllers
{
    [Authorize(Roles = "Paciente")]
    public class PacienteController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly TurnoStateService _stateService;

        public PacienteController(AppDbContext context, TurnoStateService service)
        {
            _context = context;
            _stateService = service;
        }

        // GET: Paciente/Dashboard (Vista por defecto - Mis Turnos)
        public async Task<IActionResult> Dashboard() //IActionResult representa cualquier respuesta HTTP valida, una vista, un JSON, etc
        {
            
            
            var paciente = await _context.pacientes
                .Include(p => p.idNavigation)
                .FirstOrDefaultAsync(p => p.id == UserId);

            if (paciente == null)
                return NotFound();

            // Obtener próximos turnos
            var turnos = await _context.turnos
                .Where(t => t.paciente_id == UserId && 
                           t.estado == "Reservado" &&
                           t.fecha >= DateOnly.FromDateTime(DateTime.Now))
                .Include(t => t.medico)
                    .ThenInclude(m => m.idNavigation)
                .Include(t => t.especialidad)
                .OrderBy(t => t.fecha)
                .ThenBy(t => t.hora)
                .Select(t => new TurnoViewDTO
                {
                    Id = t.id,
                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                    Estado = t.estado,
                    MedicoNombre = t.medico.idNavigation.nombre,
                    Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad"
                })
                .Take(10)
                .ToListAsync();

            ViewBag.PacienteNombre = paciente.idNavigation.nombre;
            return View(turnos);
        }

        // GET: Paciente/SolicitarTurno
        public async Task<IActionResult> SolicitarTurno()
        {
            ViewBag.PacienteNombre = await GetPacienteNombre();
            return View();
        }

        // GET: Paciente/ReprogramarTurno
        public async Task<IActionResult> ReprogramarTurno()
        {

            var turnos = await _context.turnos
                .Where(t => t.paciente_id == UserId &&
                           t.estado == "Reservado" &&
                           t.fecha >= DateOnly.FromDateTime(DateTime.Now))
                .Include(t => t.medico)
                    .ThenInclude(m => m.idNavigation)
                .Include(t => t.especialidad)
                .Select(t => new TurnoViewDTO
                {
                    Id = t.id,
                    Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                    Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                    Estado = t.estado,
                    MedicoNombre = t.medico.idNavigation.nombre,
                    PacienteNombre = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad"
                })
                .ToListAsync();

            ViewBag.PacienteNombre = await GetPacienteNombre();
            return View(turnos);
        }

        //GET: Paciente/SeleccionarTurnoCancelar
        public async Task<IActionResult> SeleccionarTurnoCancelar()
        {
            ViewBag.PacienteNombre = UserName;
            
            return View();
        }

        // GET: Paciente/CancelarTurno
      /*  public async Task<IActionResult> CancelarTurno(int? id)
        {
            if (id == null)
            {
                var turnos = await _context.turnos
                    .Where(t => t.paciente_id == UserId &&
                               t.estado == "Reservado")
                    .Include(t => t.medico)
                    .ThenInclude(m => m.idNavigation)
                    .Include(t => t.especialidad)
                    .Where(t => t.fecha.Value.ToDateTime(t.hora.Value) > DateTime.Now.AddHours(24))
                    .Select(t => new TurnoViewDTO
                    {
                        Id = t.id,
                        Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dd/MM/yyyy") : "Sin fecha",
                        Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                        Estado = t.estado,
                        MedicoNombre = t.medico.idNavigation.nombre,
                        Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad"
                    })
                    .ToListAsync();

                ViewBag.PacienteNombre = await GetPacienteNombre();
                return View("SeleccionarTurnoCancelar", turnos);
            }

            var turno = await _context.turnos
                        .Where(t => t.id == id)
                        .Include(t => t.medico)
                        .ThenInclude(m => m!.idNavigation)
                        .Include(t => t.paciente)
                        .ThenInclude(p => p!.idNavigation)
                        .FirstOrDefaultAsync();

            if (turno == null)
            {
                return NotFound();
            }

            if (!_stateService.PuedeCancelar(turno))
            {
                TempData["ErrorMessage"] = $"Este turno no puede ser cancelado. Estado actual: {turno.estado}";
                return RedirectToAction(nameof(Dashboard));
            }

            var turnoDto = new TurnoCancelDTO // <-- Usamos el nuevo DTO
            {
                Id = turno.id,
                Fecha = turno.fecha.HasValue ? turno.fecha.Value.ToString() : "Sin fecha",
                Hora = turno.hora.HasValue ? turno.hora.Value.ToString() : "Sin hora",
                PacienteNombre = (turno.paciente != null && turno.paciente.idNavigation != null) ? turno.paciente.idNavigation.nombre : "Desconocido",
                MedicoNombre = (turno.medico != null && turno.medico.idNavigation != null) ? turno.medico.idNavigation.nombre : "Desconocido"
                // La propiedad MotivoCancelacion se deja , la llena el usuario
            };

            ViewBag.PacienteNombre = UserName;
            // Devolvemos la vista de confirmación con el DTO lleno
            return View("ConfirmarCancelacion", turnoDto);
        }

        [HttpPost]
        public async Task<IActionResult> CancelarTurno(int? id, TurnoCancelDTO dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.MotivoCancelacion))
            {
                var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == dto.Id);
                if (turno == null) return NotFound();

                turno.motivo_cancelacion = dto.MotivoCancelacion;
                _context.Update(turno);

                if (_stateService.PuedeCancelar(turno))
                    _stateService.Cancelar(turno, dto.MotivoCancelacion);

                _context.Update(turno);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Dashboard));

            }
            //ModelState.AddModelError("","Debe llenar el campo del motivo de cancelacion");
            var turno2 = await _context.turnos
            .Include(t => t.paciente)
            .ThenInclude(p => p.idNavigation)
            .Include(t => t.medico)
            .ThenInclude(m => m.idNavigation)
            .FirstOrDefaultAsync(t => t.id == dto.Id);


            if (turno2 == null) return NotFound();

            dto.Fecha = turno2.fecha.ToString();
            dto.Hora = turno2.hora.ToString();
            dto.PacienteNombre = turno2.paciente.idNavigation.nombre;
            dto.MedicoNombre = turno2.medico.idNavigation.nombre;
            return View("ConfirmarCancelacion",dto);
        }
        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarTurno(int id)
        {
            var turno = await _context.turnos.FirstOrDefaultAsync(t => t.id == id);
            if (turno == null) return NotFound();

            if (_stateService.PuedeCancelar(turno))
            {
                _stateService.Cancelar(turno);
            }
        }
        */
        // GET: Paciente/HistoriaClinica
        public async Task<IActionResult> HistoriaClinica()
        {
            
            var historiaClinica = await _context.historiasclinicas
                .Include(h => h.EntradasClinicas)
                    .ThenInclude(e => e.medico)
                        .ThenInclude(m => m.idNavigation)
                .FirstOrDefaultAsync(h => h.paciente_id == UserId);

            ViewBag.PacienteNombre = await GetPacienteNombre();
            return View(historiaClinica);
        }

        // GET: Paciente/Perfil
        public async Task<IActionResult> Perfil()
        {
            
            var paciente = await _context.pacientes
                .Include(p => p.idNavigation)
                .FirstOrDefaultAsync(p => p.id == UserId);

            if (paciente == null)
                return NotFound();

            ViewBag.PacienteNombre = paciente.idNavigation.nombre;
            return View(paciente);
        }

        private async Task<string> GetPacienteNombre()
        {
            var paciente = await _context.pacientes
                .Include(p => p.idNavigation)
                .FirstOrDefaultAsync(p => p.id == UserId);
            
            return paciente?.idNavigation.nombre ?? "Usuario";
        }
    }
}