using Microsoft.AspNetCore.Mvc;
using MedCenter.Data; // Adjust namespace as needed
using MedCenter.Models; // Adjust namespace as needed
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedCenter.DTOs;
using System.Xml;
using MedCenter.Services.TurnoSv; // Assuming you have a DTO for creating/updating Medicos


namespace MedCenter.Controllers
{

    public class MedicoController : BaseController //Es el controlador de medicos para mostrarlos cuando creas/editas turnos
                                               //y ademas, se guarda/edita/elimina en su lista de turnos de medico
    {
        private readonly AppDbContext _context;
        private readonly TurnoService _turnoService;
        public MedicoController(AppDbContext context, TurnoService turnoService)
        {
            _context = context;
            _turnoService = turnoService;
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
            ViewBag.TurnosFinalizados = turnosHoy.Where(t => t.Estado == "Finalizado").Count();

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
        


        // ========== API ENDPOINTS (sin vista, devuelven JSON) ==========
        // GET: api/medicos
        //[HttpGet]
        [HttpGet("api/medicos")]
        public ActionResult<IEnumerable<MedicoViewDTO>> GetAll()
        {
            var medicos = _context.medicos
             .Include(m => m.idNavigation)
             .Include(m => m.medicoEspecialidades)
                 .ThenInclude(me => me.especialidad)
             .Select(m => new
             {
                 Nombre = m.idNavigation.nombre,
                 Email = m.idNavigation.email,
                 Matricula = m.matricula,
                 Especialidades = m.medicoEspecialidades
                    .Select(me => me.especialidad.nombre ?? "Sin nombre")
                    .ToList()
             })
                .ToList();

            return Ok(medicos);
        }

        [HttpGet("api/medicos/{id}")]
        public ActionResult<MedicoViewDTO> GetById(int id)
        {
            var medicoDTO = _context.medicos
            .Where(m => m.id == id)
            .Select(m => new MedicoViewDTO
            {
                Id = m.id,
                Matricula = m.matricula,
                Nombre = m.idNavigation.nombre,
                Email = m.idNavigation.email,
                Especialidades = m.medicoEspecialidades.Select(me => me.especialidad.nombre!).ToList(),
                //TurnosIds = m.turnos.Select(t => t.id).ToList()
            })
            .FirstOrDefault();



            if (medicoDTO == null)
            {
                return NotFound();
            }

            return Ok(medicoDTO);
        }

        // POST: api/medicos
        [HttpPost("api/medicos")]
        public ActionResult<Medico> Create([FromBody] Medico newMedico)
        {
            if (newMedico == null)
            {
                return BadRequest();
            }
            _context.medicos.Add(newMedico);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { id = newMedico.id }, newMedico);
        }

        // PUT: api/medicos/{id}
        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] Medico updatedMedico)
        {
            var medico = _context.medicos
            .Include(m => m.idNavigation)
            .Include(m => m.medicoEspecialidades)
                .ThenInclude(me => me.especialidad)
            .FirstOrDefault(m => m.id == id);

            if (medico == null)
                return NotFound();

            return Ok(new
            {
                Id = medico.id,
                Nombre = medico.idNavigation.nombre,
                Email = medico.idNavigation.email,
                Matricula = medico.matricula,
                Especialidades = medico.medicoEspecialidades.Select(me => me.especialidad.nombre).ToList()
            });
        }

        // DELETE: api/medicos/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var medico = _context.medicos
                .Include(m => m.idNavigation)
                .Include(m => m.medicoEspecialidades)
                .FirstOrDefault(m => m.id == id);

            if (medico == null)
                return NotFound();

            _context.medicoEspecialidades.RemoveRange(medico.medicoEspecialidades);
            _context.personas.Remove(medico.idNavigation); // elimina también la persona asociada
            _context.medicos.Remove(medico);

            _context.SaveChanges();
            return NoContent();
        }
        


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