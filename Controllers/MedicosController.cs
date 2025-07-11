using Microsoft.AspNetCore.Mvc;
using MedCenter.Data; // Adjust namespace as needed
using MedCenter.Models; // Adjust namespace as needed
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedCenter.DTOs; // Assuming you have a DTO for creating/updating Medicos


namespace MedCenter.Controllers
{

    public class MedicosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MedicosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/medicos
        [HttpGet]
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


        public ActionResult<MedicoViewDTO> GetById(int id)
        {
            var medicoDTO = _context.medicos
            .Where(m => m.id == id)
            .Select(m => new MedicoViewDTO
            {
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
        [HttpPost]
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