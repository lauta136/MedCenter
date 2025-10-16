using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.DTOs;
using System.Security.Claims;

namespace MedCenter.Controllers
{
    [Authorize(Roles = "Secretaria")]

    public class SecretariaController : BaseController
    {
        private AppDbContext _context;
        private AuthService _authService;

        public SecretariaController(AppDbContext context, AuthService auth)
        {
            _context = context;
            _authService = auth;
        }

       

        //GET: Secretaria:Dashboard Gestion de Turnos-Vista por defecto
        public async Task<IActionResult> Dashboard()
        {
            

            var secretaria = await _context.secretarias.FirstOrDefaultAsync(s => s.id == UserId); //Puede haber error

            if (secretaria == null)
                return NotFound();

            var turnosHoy = await _context.turnos.Where(t => t.estado == "Reservado" && t.fecha.Value.Day == DateTime.Now.Day)
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
                         }).
                         ToListAsync();

            // Estadísticas
            ViewBag.TotalTurnosHoy = turnosHoy.Count();
            ViewBag.TurnosConfirmados = turnosHoy.Count(t => t.Estado == "Reservado");
            ViewBag.TurnosCancelados = await _context.turnos.CountAsync(t => t.estado == "Cancelado");
            ViewBag.TotalPacientes = await _context.pacientes.CountAsync();
            ViewBag.SecretariaNombre = UserName;


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
            ViewBag.SecretariaNombre = UserName;
            return View(pacientes);
        }

        //GET: Secretaria/NuevoPaciente
        public async Task<IActionResult> NuevoPaciente()
        {
            ViewBag.SecretariaNombre = UserName;

            return View(new RegisterDTO { Role = "Paciente" }); //Para que en el POST ya tenga el rol correcto

        }

        // POST: Secretaria/NuevoPaciente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevoPaciente(RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.SecretariaNombre = UserName;
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
            ViewBag.SecretariaNombre = UserName;
            return View(model);
        }


        // GET: Secretaria/AgendaMedica
        public async Task<IActionResult> AgendaMedica()
        {
            var medicos = await _context.medicos
                .Include(m => m.idNavigation)
                .Include(m => m.medicoEspecialidades)
                    .ThenInclude(me => me.especialidad)
                .Select(m => new MedicoViewDTO
                {
                    Id = m.id,
                    Nombre = m.idNavigation.nombre,
                    Matricula = m.matricula,
                    Especialidades = m.medicoEspecialidades
                        .Select(me => me.especialidad.nombre ?? "Sin especialidad")
                        .ToList()
                })
                .ToListAsync();

            ViewBag.SecretariaNombre = UserName;
            return View(medicos);
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
                .Where(t => t.fecha >= inicioMes && t.fecha <= hoy && t.estado == "Cancelado")
                .CountAsync();

            ViewBag.TurnosMes = turnosMes;
            ViewBag.PacientesNuevosMes = pacientesNuevosMes;
            ViewBag.TurnosCancelados = turnosCancelados;
            ViewBag.SecretariaNombre = UserName;

            return View();
        }

        // GET: Secretaria/SolicitudesTurnos
        public async Task<IActionResult> SolicitudesTurnos()
{
    // TODO: Implementar lógica para obtener solicitudes pendientes de pacientes
    // Filtrar turnos con estado "Pendiente" o similar
    
    ViewBag.SecretariaNombre = UserName;
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
    
    ViewBag.SecretariaNombre = UserName;
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