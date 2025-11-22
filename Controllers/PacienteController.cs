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
                           (t.estado == "Reservado" || t.estado == "Reprogramado") &&
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

            ViewBag.UserName = UserName;
            return View(turnos);
        }

        // GET: Paciente/SolicitarTurno
        public async Task<IActionResult> SolicitarTurno()
        {
            ViewBag.UserName = UserName;
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

            ViewBag.UserName = UserName;
            return View(turnos);
        }

        //GET: Paciente/SeleccionarTurnoCancelar
        public async Task<IActionResult> SeleccionarTurnoCancelar()
        {
            ViewBag.UserName = UserName;
            
            return View();
        }

        // GET: Paciente/HistoriaClinica
        public async Task<IActionResult> HistoriaClinica()
        {
            
            var historiaClinica = await _context.historiasclinicas
                .Include(h => h.EntradasClinicas)
                    .ThenInclude(e => e.medico)
                        .ThenInclude(m => m.idNavigation)
                .FirstOrDefaultAsync(h => h.paciente_id == UserId);

            ViewBag.UserName = UserName;
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

            ViewBag.UserName = paciente.idNavigation.nombre;
            return View(paciente);
        }

       
    }
}