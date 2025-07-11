using Microsoft.AspNetCore.Mvc;
using MedCenter.Data; // Adjust namespace as needed

using MedCenter.Models; // Adjust namespace as needed


public class TurnosController : ControllerBase
{
        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        /*GET: api/turnos
        [HttpGet]
        public ActionResult<IEnumerable<Turnos>> GetAll()
        {
            var turnos = _context.turnos.
                .Include(t => t.paciente)
                .Include(t => t.medico)
                    .ThenInclude(m => m.idNavigation)
                .Include(t => t.slotAgenda)
                .Select(t => new
                {
                    Id = t.id,
                    PacienteNombre = t.paciente.nombre,
                    MedicoNombre = t.medico.idNavigation.nombre,
                    FechaHora = t.slotAgenda.fechaHora,
                    Estado = t.estado
                })
                .ToList();

            return Ok(turnos);
        }
        */

}