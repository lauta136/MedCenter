// Agregar este método a tu TurnoController o crear un EspecialidadController

using MedCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class EspecialidadController : Controller
{
    private readonly AppDbContext _context;

    public EspecialidadController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<JsonResult> GetEspecialidades()
    {
        var especialidades = await _context.especialidades
                                          .Select(e => new { e.id, e.nombre })
                                          .ToListAsync();
        return Json(especialidades);
    }
}