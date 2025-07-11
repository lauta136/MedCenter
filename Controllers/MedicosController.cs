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
        public ActionResult<IEnumerable<Medicos>> GetAll()
        {
            var medicos = _context.medicos
             .Include(m => m.idNavigation)
             .Include(m => m.medicoEspecialidades)
                 .ThenInclude(me => me.especialidad)
             .Select(m => new
             {
                 Id = m.id,
                 Nombre = m.idNavigation.nombre,
                 Email = m.idNavigation.email,
                 Matricula = m.matricula,
                 Especialidades = m.medicoEspecialidades.Select(me => me.especialidad.nombre).ToList()
             })
             .ToList();

            return Ok(medicos);
        }


        public ActionResult<Medicos> GetById(int id)
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

        // POST: api/medicos
        [HttpPost]
        public ActionResult<Medicos> Create([FromBody] Medicos newMedico)
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
        public ActionResult Update(int id, [FromBody] Medicos updatedMedico)
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

        // POST: api/medicos
    [HttpPost]
    public IActionResult Create([FromBody] UpdateMedicoDto dto)
    {
        var persona = new Personas
        {
            nombre = dto.Nombre,
            email = dto.Email,
            contraseña = "temporal" // podés ajustar esto
        };

        var medico = new Medicos
        {
            matricula = dto.Matricula,
            idNavigation = persona
        };

        _context.medicos.Add(medico);
        _context.SaveChanges(); // guardamos primero para obtener el ID

        foreach (var espId in dto.EspecialidadesIds)
        {
            _context.medicoEspecialidades.Add(new MedicoEspecialidad
            {
                medicoId = medico.id,
                especialidadId = espId
            });
        }

        _context.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = medico.id }, null);
    }
    
    }
}