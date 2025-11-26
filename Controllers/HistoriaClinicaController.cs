using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.HistoriaClinicaSv;
using MedCenter.Services.TurnoSv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Controllers;

public class HistoriaClinicaController : BaseController
{
    private readonly TurnoService _turnoService;
    private readonly AppDbContext _context;

    private readonly HistoriaClinicaService _historiaService;

    public HistoriaClinicaController(TurnoService turnoService, AppDbContext appDbContext, HistoriaClinicaService historiaClinicaService)
    {
        _turnoService = turnoService;
        _context = appDbContext;
        _historiaService = historiaClinicaService;
    }

    [HttpGet]
    public async Task<IActionResult> NuevaEntrada(int paciente_id, bool verificar = false)
    {
        ViewBag.UserName = UserName;
        
        Turno turno = await _turnoService.GetTurnoActual(paciente_id, UserId.Value);
        if(turno == null) 
            return Json(new {success = false, message = "No está atendiendo al paciente, no puede registrar una entrada clínica"});

        // If verificar=true, just return success (for AJAX check)
        if (verificar)
            return Json(new {success = true});

        Paciente paciente= await _context.pacientes
            .Include(p => p.idNavigation)
            .Where(p => p.id == paciente_id)
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

    [HttpPost]
    public async Task<IActionResult> NuevaEntrada(EntradaClinicaRegisterDTO entrada, int turno_id)
    {
        var result = await _historiaService.GuardarNuevaEntrada(entrada, turno_id);
        return Json(new {success = result.success, message = result.message});
    }

   [HttpGet] //quizas no usado, revisar mas adelante
    public async Task<IActionResult> HistoriaClinica(int paciente_id)
    {
        ViewBag.UserName = UserName;
        MedCenter.Models.HistoriaClinica historia = await _historiaService.GetHistoriaClinicaPaciente(paciente_id);
        
        if(historia != null)
        {
            return Json(new {success = true, entradas = historia.EntradasClinicas});
        }

        return Json(new {success = false});
    }
    
    [HttpGet]
    public async Task<IActionResult> TodasLasHistorias()
    {
        int medico_id = UserId.Value;
        ViewBag.UserName = UserName;
        List<HistoriaClinicaViewDTO> historias = await _historiaService.GetTodasHistoriasClinicas(medico_id);
        return View("~/Views/Medico/HistoriasClinicas.cshtml", historias);
    }

    [HttpGet]
    public async Task<IActionResult> VerHistoria(int paciente_id)
    {
        ViewBag.UserName = UserName;
        HistoriaClinicaViewDTO historia = await _historiaService.VerHistoriaClinica(paciente_id);
        
        return View("~/Views/Medico/VerHistoria.cshtml", historia);
    }

    [HttpPost]
    public async Task<IActionResult> CrearHistoria(int paciente_id)
    {
        var result  = await _historiaService.CrearHistoriaClinica(paciente_id);
        return Json(new{success = result.success, message = result.message});
    }
}