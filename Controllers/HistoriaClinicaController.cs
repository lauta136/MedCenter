using MedCenter.Data;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Controllers;

public class HistoriaClinicaController : BaseController
{
    private readonly TurnoService _turnoService;
    private readonly AppDbContext _context;

    public HistoriaClinicaController(TurnoService turnoService, AppDbContext appDbContext)
    {
        _turnoService = turnoService;
        _context = appDbContext;
    }

    [HttpGet]
    public async Task<IActionResult> NuevaEntrada(int paciente_id)
    {
        ViewBag.UserName = UserName;
        
        Turno turno = await _turnoService.GetTurnoActual(paciente_id, UserId.Value);
        if(turno == null) return NotFound();

        Paciente paciente= await _context.pacientes
            .Include(p => p.idNavigation)
            .Where(p => p.id == paciente_id)
            //.Select(p => p.idNavigation.nombre)
            .FirstOrDefaultAsync();

        if(paciente == null) return NotFound();

        // Pass data via ViewBag
        ViewBag.PacienteNombre = paciente.idNavigation.nombre;
        ViewBag.PacienteId = paciente.id;
        ViewBag.TurnoId = turno.id;
        ViewBag.FechaTurno = turno.fecha?.ToString("dd/MM/yyyy");
        ViewBag.HoraTurno = turno.hora?.ToString(@"HH\:mm");

        return View("~/Views/Medico/NuevaEntrada.cshtml");
    }
}