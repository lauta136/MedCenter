using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.DTOs;
using System.Security.Claims;
using MedCenter.Migrations;
using MedCenter.Models;
using System.Runtime.InteropServices;
using MedCenter.Services.DisponibilidadMedico;
using MedCenter.Services.TurnoSv;
using MedCenter.Services.TurnoStates;

namespace MedCenter.Controllers
{
    [Authorize(Roles = "Secretaria")]

    public class SecretariaController : BaseController
    {
        private AppDbContext _context;
        private AuthService _authService;
        private readonly TurnoStateService _stateService;
        private DisponibilidadService _dispoService;
        private readonly TurnoService _turnoService;


        public SecretariaController(AppDbContext context, AuthService auth, DisponibilidadService disponibilidadService, TurnoStateService turnoStateService, TurnoService turnoService)
        {
            _context = context;
            _authService = auth;
            _dispoService = disponibilidadService;
            _stateService = turnoStateService;
            _turnoService = turnoService;
        }

       

        //GET: Secretaria:Dashboard Gestion de Turnos-Vista por defecto
        public async Task<IActionResult> Dashboard()
        {
            

            var secretaria = await _context.secretarias.FirstOrDefaultAsync(s => s.id == UserId); //Puede haber error

            if (secretaria == null)
                return NotFound();

            var turnosHoy = await _context.turnos.Where(t => t.estado == EstadosTurno.Reservado.ToString() && t.fecha.Value.Day == DateTime.Now.Day)
                         .OrderBy(t => t.hora)
                         .Include(t => t.medico)
                         .ThenInclude(m => m.idNavigation)
                         .Include(t => t.paciente)
                         .ThenInclude(p => p.idNavigation)
                         .Select(t => new TurnoViewDTO
                         {
                             Id = t.id,
                             Hora = t.hora.HasValue ? t.hora.Value.ToString(@"HH\:mm") : "Sin hora",
                             Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("dddd/MM/yyyy") : "Sin fecha",
                             Especialidad = t.especialidad != null ? t.especialidad.nombre : "Sin especialidad",
                             Estado = t.estado,
                             MedicoNombre = t.medico.idNavigation.nombre,
                             PacienteNombre = t.paciente.idNavigation.nombre,
                             PuedeReprogramar = _stateService.PuedeReprogramar(t),
                             PuedeCancelar = _stateService.PuedeCancelar(t)
                         }).
                         ToListAsync();

            // Estadísticas
            ViewBag.TotalTurnosHoy = turnosHoy.Count();
            ViewBag.TurnosConfirmados = turnosHoy.Count(t => t.Estado == EstadosTurno.Reservado.ToString());
            ViewBag.TurnosCancelados = await _context.turnos.CountAsync(t => t.estado == EstadosTurno.Cancelado.ToString());
            ViewBag.TotalPacientes = await _context.pacientes.CountAsync();
            ViewBag.UserName = UserName;

            await _turnoService.FinalizarAusentarTurnosPasados();


            return View(turnosHoy);
        }

        // GET: Secretaria/AsignarTurno
        public async Task<IActionResult> AsignarTurno()
        {
            return View();
        }

        // GET: Secretaria/AsignarTurno
        public async Task<IActionResult> CancelarTurno(int? id)
        {
            return View();
        }

        // GET: Secretaria/ConsultarDisponibilidad
        public async Task<IActionResult> ConsultarDisponibilidad()
        {
            var especialidades = await _context.especialidades.ToListAsync();
            ViewBag.UserName = UserName;
            return View(especialidades);
        }

        // GET: Secretaria/Pacientes
        public async Task<IActionResult> Pacientes()
        {
            var pacientes = await _context.pacientes.Include(p => p.idNavigation)
                            .Select(p => new PacienteViewDTO
                            {
                                Nombre = p.idNavigation.nombre,
                                Dni = p.dni,
                                Telefono = p.telefono,
                                Email = p.idNavigation.email
                            })
                            .ToListAsync();
            ViewBag.UserName = UserName;
            return View(pacientes);
        }

        //GET: Secretaria/NuevoPaciente
        public async Task<IActionResult> NuevoPaciente()
        {
            ViewBag.UserName = UserName;

            return View(new RegisterDTO { Role = "Paciente" }); //Para que en el POST ya tenga el rol correcto

        }

        // POST: Secretaria/NuevoPaciente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevoPaciente(RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.UserName = UserName;
                return View(model);
            }

            // Asegurarse de que el rol sea Paciente
            model.Role = "Paciente";

            // Reusar el servicio de autenticación
            var result = await _authService.RegisterAsync(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Paciente registrado exitosamente";
                return RedirectToAction("Pacientes");
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Error al registrar el paciente");
            ViewBag.UserName = UserName;
            return View(model);
        }


        // GET: Secretaria/AgendaMedica
        public async Task<IActionResult> AgendaMedico()
        {
            var medicos = await _context.medicos
                .Include(m => m.idNavigation)
                .Include(m => m.disponibilidadesMedico)
                //.Include(m => m.medicoEspecialidades)
                    //.ThenInclude(me => me.especialidad)
                    .Include(m => m.slotsAgenda)
                .Select(m => new MedicoAgendaDTO(
                    m.id,
                    m.idNavigation.nombre!,
                    m.matricula!,
                    m.slotsAgenda,
                    m.disponibilidadesMedico
                ))
                .ToListAsync();

            ViewBag.UserName = UserName;
            return View(medicos);
        }

        [HttpGet]
        public async Task<IActionResult> GestionarDisponibilidad(int medico_id)
        {
            var medico = await _context.medicos
                    .Include(m => m.idNavigation)
                    .FirstOrDefaultAsync(m => m.id == medico_id);

            if (medico == null)
                return NotFound();

            var disponibilidad = await _context.disponibilidad_medico
            .Where(dm => dm.medico_id == medico_id && dm.activa == true)
            .OrderBy(dm => dm.dia_semana)
            .ThenBy(dm => dm.hora_inicio)
            .ToListAsync();

            ViewBag.UserName = UserName;
            ViewBag.MedicoId = medico_id;
            ViewBag.MedicoNombre = medico.idNavigation.nombre;

            return View(disponibilidad);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarBloqueDisponibilidad(int medico_id, ManipularDisponibilidadDTO dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Verifique los campos.";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id }); //agrega nombre de la vista al que lo mandas
            }

            DisponibilidadResult result = await _dispoService.AgregarBloqueDisponibilidad(medico_id, dto);

            if(result.success)
            {
                TempData["SuccessMessage"] = result.message;
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id});
            }

        }

       /* [HttpPost]
        public async Task<IActionResult> EditarBloqueDisponibilidad(ManipularDisponibilidadDTO dto, int dispo_id, int medico_id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Verifique los campos.";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id });
            }
            
            DisponibilidadResult result = await _dispoService.EditarBloqueDisponibilidad(dto, dispo_id, medico_id);

            if (result.success)
            {
                TempData["SuccessMessage"] = result.message;
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            
        }
        */
        [HttpPost]
        public async Task<IActionResult> CancelarBloqueDisponibilidad(int medico_id, ManipularDisponibilidadDTO dto, int dispo_id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Error inesperado al cancelar el bloque";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id }); //agrega nombre de la vista al que lo mandas
            }

            DisponibilidadResult result = await _dispoService.CancelarBloqueDisponibilidad(medico_id, dto, dispo_id);

            if (result.success)
                TempData["SuccessMessage"] = "El bloque fue cancelado exitosamente";

            TempData["ErrorMessage"] = result.message;
            
            return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
             
        }

        public async Task<IActionResult> GenerarSlotsAgenda(int medico_id)
        {
            DisponibilidadResult result = await _dispoService.GenerarSlotsAgenda(medico_id);

            return Json(new {success = result.success, message = result.message});
        }
        
        // GET: Secretaria/ObrasSociales
        public async Task<IActionResult> ObrasSociales()
        {
            var obrasSociales = await _context.obras_sociales
                .OrderBy(os => os.nombre)
                .ToListAsync();

            ViewBag.UserName = UserName;
            return View(obrasSociales);
        }

        // GET: Secretaria/Reportes
        public async Task<IActionResult> Reportes()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

            // Estadísticas del mes
            var turnosMes = await _context.turnos
                .Where(t => t.fecha >= inicioMes && t.fecha <= hoy)
                .CountAsync();

            var pacientesNuevosMes = await _context.pacientes
                .CountAsync(); // Aquí deberías filtrar por fecha de creación si tienes ese campo

            var turnosCancelados = await _context.turnos
                .Where(t => t.fecha >= inicioMes && t.fecha <= hoy && t.estado == EstadosTurno.Cancelado.ToString())
                .CountAsync();

            ViewBag.TurnosMes = turnosMes;
            ViewBag.PacientesNuevosMes = pacientesNuevosMes;
            ViewBag.TurnosCancelados = turnosCancelados;
            ViewBag.UserName = UserName;

            return View();
        }

        // GET: Secretaria/SolicitudesTurnos
        public async Task<IActionResult> SolicitudesTurnos()
{
    // TODO: Implementar lógica para obtener solicitudes pendientes de pacientes
    // Filtrar turnos con estado "Pendiente" o similar
    
    ViewBag.UserName = UserName;
    return View();
}

        // POST: Secretaria/AprobarTurno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarTurno(int turnoId)
{
    // TODO: Implementar lógica para aprobar solicitud de turno
    // Cambiar estado del turno a "Reservado"
    
    TempData["SuccessMessage"] = "Turno aprobado exitosamente";
    return RedirectToAction("Dashboard");
}

        // POST: Secretaria/RechazarTurno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarTurno(int turnoId, string motivo)
        {
            // TODO: Implementar lógica para rechazar solicitud de turno
            // Cambiar estado del turno a "Rechazado" o eliminar
            // Notificar al paciente del rechazo con el motivo
    
            TempData["SuccessMessage"] = "Turno rechazado";
            return RedirectToAction("Dashboard");
        }

// GET: Secretaria/SolicitudesReprogramacion
public async Task<IActionResult> SolicitudesReprogramacion()
{
    // TODO: Implementar lógica para obtener solicitudes de reprogramación
    // Necesitarás una tabla adicional para guardar estas solicitudes
    
    ViewBag.UserName = UserName;
    return View();
}

// POST: Secretaria/AprobarReprogramacion
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AprobarReprogramacion(int solicitudId)
{
    // TODO: Implementar lógica para aprobar reprogramación
    // Actualizar el turno con la nueva fecha/hora
    // Marcar solicitud como procesada
    
    TempData["SuccessMessage"] = "Reprogramación aprobada exitosamente";
    return RedirectToAction("Dashboard");
}

// POST: Secretaria/RechazarReprogramacion
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RechazarReprogramacion(int solicitudId, string motivo)
{
    // TODO: Implementar lógica para rechazar reprogramación
    // Mantener el turno original
    // Notificar al paciente del rechazo
    
    TempData["SuccessMessage"] = "Reprogramación rechazada";
    return RedirectToAction("Dashboard");
}
    }
}