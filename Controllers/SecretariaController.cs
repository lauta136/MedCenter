using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.DTOs;
using System.Security.Claims;
using MedCenter.Migrations;
using MedCenter.Models;
using System.Runtime.InteropServices;

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

            ViewBag.SecretariaNombre = UserName;
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

            ViewBag.SecretariaNombre = UserName;
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
                return RedirectToAction(nameof(GestionarDisponibilidad), new {medico_id = medico_id }); //agrega nombre de la vista al que lo mandas
            }

            if (await NuevaDisponibilidadCoherente(dto, medico_id))
            {
                _context.disponibilidad_medico.Add(new DisponibilidadMedico
                {
                    medico_id = medico_id,
                    dia_semana = dto.Dia_semana,
                    hora_inicio = dto.Hora_inicio,
                    hora_fin = dto.Hora_fin,
                    vigencia_desde = DateOnly.FromDateTime(DateTime.Now),
                    duracion_turno_minutos = dto.Duracion_turno_minutos
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "El bloque fue agregado exitosamente";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            else
            {
                TempData["ErrorMessage"] = "El nuevo horario ingresado se superpone con uno ya activo";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id});
            }

        }

        [HttpPost]
        public async Task<IActionResult> EditarBloqueDisponibilidad(ManipularDisponibilidadDTO dto, int dispo_id, int medico_id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Datos inválidos. Verifique los campos.";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id });
            }
            

            if (await NuevaDisponibilidadCoherente(dto, medico_id))
            {
                var dispo = await _context.disponibilidad_medico.FirstOrDefaultAsync(dm => dm.id == dispo_id);

                if (dispo == null) return NotFound();

                dispo.dia_semana = dto.Dia_semana;
                dispo.hora_inicio = dto.Hora_inicio;
                dispo.hora_fin = dto.Hora_fin;
                dispo.duracion_turno_minutos = dto.Duracion_turno_minutos;

                _context.Update(dispo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "El bloque fue actualizado exitosamente";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            else
            {
                TempData["ErrorMessage"] = "El nuevo horario ingresado se superpone con uno ya activo";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
            }
            
        }
        
         [HttpPost]
        public async Task<IActionResult> CancelarBloqueDisponibilidad(int medico_id, ManipularDisponibilidadDTO dto, int dispo_id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Error inesperado al cancelar el bloque";
                return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id}); //agrega nombre de la vista al que lo mandas
            }



            var dispo = await _context.disponibilidad_medico.FirstOrDefaultAsync(dm => dm.id == dispo_id);
            if (dispo == null) return NotFound();

            dispo.activa = false;

            _context.Update(dispo);

             await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "El bloque fue cancelado exitosamente";
            return RedirectToAction(nameof(GestionarDisponibilidad), new { medico_id = medico_id });
             
        }

        public async Task<bool> NuevaDisponibilidadCoherente(ManipularDisponibilidadDTO dto, int medico_id) //Que la nueva insercion no se superponga con otra ya guardada
        {
            // bool flag = true;

            if (dto.Hora_inicio >= dto.Hora_fin) return false;

            if ((dto.Hora_fin - dto.Hora_inicio).Minutes < dto.Duracion_turno_minutos) return false;

            var disponibilidadesPrecisas = await _context.disponibilidad_medico.Where(dm => dm.medico_id == medico_id && dm.activa == true && dm.dia_semana == dto.Dia_semana)
                                                                               .ToListAsync();

            foreach (var item in disponibilidadesPrecisas)
            {
                if (dto.Hora_inicio.IsBetween(item.hora_inicio, item.hora_fin) || dto.Hora_fin.IsBetween(item.hora_inicio, item.hora_fin))
                    return false;
            }

            return true;
        }

        public async Task<IActionResult> GenerarSlotsAgenda(int medico_id)
        {
            return Ok();
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