using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Data;
using MedCenter.DTOs;

// Heredamos de Controller para poder trabajar con Vistas Razor
public class TurnoController : Controller
{
    private readonly AppDbContext _context;

    public TurnoController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Turno/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var turnoDto = await _context.turnos
            .Where(t => t.id == id)
            .Select(t => new TurnoViewDTO
            {
                Id = t.id,
                Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha",
                Hora = t.hora.HasValue ? t.hora.Value.ToString(@"hh\:mm") : "Sin hora",
                Estado = t.estado,
                PacienteNombre = (t.paciente != null && t.paciente.idNavigation != null) ? t.paciente.idNavigation.nombre : "Desconocido",
                MedicoNombre = (t.medico != null && t.medico.idNavigation != null)
                                ? t.medico.idNavigation.nombre
                                : "Desconocido"
            })
            .FirstOrDefaultAsync();

        if (turnoDto == null)
        {
            return NotFound();
        }

        // Devolvemos la vista "Details.cshtml" y le pasamos el DTO como modelo
        return View(turnoDto);
    }

    // GET: Turno
    // Este método ahora se llamará cuando navegues a /Turno o /Turno/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var turnos = await _context.turnos
            .Select(t => new TurnoViewDTO
            {
                Id = t.id,
                Fecha = t.fecha.HasValue ? t.fecha.Value.ToString("yyyy-MM-dd") : "Sin fecha",
                Hora = t.hora.HasValue ? t.hora.Value.ToString(@"hh\:mm") : "Sin hora",
                Estado = t.estado,
                PacienteNombre = (t.paciente != null && t.paciente.idNavigation != null) ? t.paciente.idNavigation.nombre : "Desconocido",
                MedicoNombre = (t.medico != null && t.medico.idNavigation != null)
                                ? t.medico.idNavigation.nombre
                                : "Desconocido"
            })
            .ToListAsync();

        // Devolvemos la vista "Index.cshtml" y le pasamos la lista de DTOs como modelo.
        // Esto conecta directamente con el @model IEnumerable<TurnoViewDTO> de tu archivo .cshtml
        return View(turnos);
    }
}