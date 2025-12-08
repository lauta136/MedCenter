using Microsoft.AspNetCore.Mvc;
using MedCenter.Data; // Adjust namespace as needed
using MedCenter.Models; // Adjust namespace as needed
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedCenter.DTOs;
using System.Xml;
using MedCenter.Services.TurnoSv;
using MedCenter.Services.MedicoSv; // Assuming you have a DTO for creating/updating Medicos


namespace MedCenter.Controllers
{

    public class MedicoController : BaseController //Es el controlador de medicos para mostrarlos cuando creas/editas turnos
                                               //y ademas, se guarda/edita/elimina en su lista de turnos de medico
    {
        private readonly AppDbContext _context;
        private readonly TurnoService _turnoService;
        private readonly MedicoService _medicoService;

        public MedicoController(AppDbContext context, TurnoService turnoService, MedicoService medicoService)
        {
            _context = context;
            _turnoService = turnoService;
            _medicoService = medicoService;
        }

        // ========== ACCIONES CON VISTA (para el dashboard) ==========

        //GET Medico/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var medico = await _context.medicos.FirstOrDefaultAsync(m => m.id == UserId); //Puede haber error

            if (medico == null)
                return NotFound();

            var turnosHoy = await _context.turnos.Where(t => t.medico_id == UserId && t.fecha.Value.Day == DateTime.Now.Day && t.estado != "Cancelado")
                            .Include(t => t.paciente)
                            .ThenInclude(p => p.idNavigation)
                            //.ThenInclude(p => p.nombre)
                            .Include(t => t.medico)
                            .ThenInclude(m => m.idNavigation)
                            //ThenInclude(p => p.nombre)
                            .Include(t => t.especialidad)
                            //.ThenInclude(e => e.nombre)
                            .Select(t => new TurnoViewDTO
                            {
                                Id = t.id,
                                Fecha = t.fecha.ToString(),
                                Hora = t.hora.ToString(),
                                Estado = t.estado,
                                PacienteNombre = t.paciente.idNavigation.nombre,
                                MedicoNombre = t.medico.idNavigation.nombre,
                                Especialidad = t.especialidad.nombre,
                                PacienteId = t.paciente_id

                            }).ToListAsync();

            ViewBag.UserName = UserName;
            ViewBag.TotalTurnosHoy = turnosHoy.Count();
            ViewBag.TurnosFinalizados = turnosHoy.Where(t => t.Estado == EstadosTurno.Finalizado.ToString()).Count();

            await _turnoService.FinalizarAusentarTurnosPasados();
            
            return View(turnosHoy);
        }

        //GET //Medico/MisPacientes`1

        public async Task<IActionResult> MisPacientes()
        {
            List<int> misPacientesIds = new List<int>();

            var pacientes = await _context.pacientes
                            .Include(p => p.turnos)
                            .ToArrayAsync();

            //misPacientes = pacientes;

            foreach (Paciente paciente in pacientes)
            {
                if (paciente.turnos != null)
                {
                    foreach (Turno turno in paciente.turnos)
                    {
                        if (turno.medico_id == UserId)
                            misPacientesIds.Add(paciente.id);
                    }
                }
            }

            var misPacientesViews = await _context.pacientes
                                    .Where(p => misPacientesIds.Contains(p.id))
                                    .Include(p => p.idNavigation)
                                    .Select(p => new PacienteViewDTO
                                    {
                                        Id = p.id,
                                        Nombre = p.idNavigation.nombre,
                                        Dni = p.dni,
                                        Telefono = p.telefono,
                                        Email = p.idNavigation.email
                                    })
                                    .ToListAsync();

            ViewBag.UserName = UserName;
            
            return View(misPacientesViews);
        }

        public async Task<IActionResult> GetAll()
        {
            var medicos = await _medicoService.GetAll();
            return Ok(medicos);
        }
        


        // ========== API ENDPOINTS (sin vista, devuelven JSON) ==========
        // GET: api/medicos
        //[HttpGet]
       

       

        
       
        


        /* POST: api/medicos
        [HttpPost]
        public IActionResult Create([FromBody] MedicoViewDTO dto)
        {
            var persona = new Persona
            {
                nombre = dto.Nombre,
                email = dto.Email,
                contraseña = "temporal" // podés ajustar esto
            };

            var medico = new Medico
            {
                matricula = dto.Matricula,
                idNavigation = persona
            };

            _context.medicos.Add(medico);
            _context.SaveChanges(); // guardamos primero para obtener el ID

            foreach (var espId in dto.Especialidades)
            {
                _context.medicoEspecialidades.Add(new MedicoEspecialidad
                {
                    medicoId = medico.id,
                    especialidad = espId
                });
            }

            _context.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = medico.id }, null);
        }
        */
    }

    
}