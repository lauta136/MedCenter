using MedCenter.Data;
using MedCenter.Models;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MedCenter.DTOs;
using MedCenter.Services.DisponibilidadMedico;
using System.Drawing;
using Microsoft.EntityFrameworkCore.Design;
using MedCenter.Services.TurnoStates;
using System.Runtime.Serialization;
using MedCenter.Migrations;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace MedCenter.Services.TurnoSv;

public class TurnoService
{
    private readonly AppDbContext _context;
    private readonly TurnoStateService _stateService;
    private readonly DisponibilidadService _dispoService;

    public TurnoService(AppDbContext appDbContext, TurnoStateService turnoStateService, DisponibilidadService disponibilidadService)
    {
        _context = appDbContext;
        _stateService = turnoStateService;
        _dispoService = disponibilidadService;
    }
    public async Task FinalizarAusentarTurnosPasados()
    {
        // Use the fully-qualified model type to avoid the Turno namespace/type conflict
        // and compare only by date here (time comparison omitted because the property was missing in the original expression).
       
        List<Turno> turnosPasados =await  _context.turnos.Include(t => t.slot). Include(t => t.medico).ThenInclude(m => m.idNavigation)
                                                  .Include(t => t.paciente).ThenInclude(p => p.idNavigation)
                                                  .Include(t => t.especialidad)
                                                  .Include(t => t.EntradaClinica)
                                                  .Where(t => t.slot != null && t.slot.disponible == false && t.fecha.Value.ToDateTime(t.slot.horafin.Value).AddMinutes(30) < DateTime.Now
                                                   && (t.estado == "Reservado"|| t.estado == "Reprogramado"))
                                                  .ToListAsync();
            
        List<Turno> turnosAAusentar = turnosPasados.Where(t => t.EntradaClinica == null).ToList();
        List<Turno> turnosAFinalizar = turnosPasados.Where(t => t.EntradaClinica != null).ToList();


        foreach(Turno turno in turnosAFinalizar)
        {
            RegistrarCambioEstadoTerminal(turno, AccionesTurno.FINALIZE.ToString(),EstadosTurno.Finalizado.ToString(),"El turno ha finalizado", RolUsuario.System.ToString());
            //_dispoService.LiberarSlot(turno.slot_id.Value);
            turno.slot_id = null;
            _stateService.Finalizar(turno);
        }

        foreach(Turno turno in turnosAAusentar)
        {
            RegistrarCambioEstadoTerminal(turno, AccionesTurno.NOSHOW.ToString(),EstadosTurno.Ausentado.ToString(),"El paciente no atendio al turno", RolUsuario.System.ToString());
            //_dispoService.LiberarSlot(turno.slot_id.Value);
            turno.slot_id = null;
            _stateService.MarcarAusente(turno);
        }

        await _context.SaveChangesAsync();

    }

    private void RegistrarCambioEstadoTerminal(Turno turno, string accion, string estadoNuevo, string descripcion, string usuarioNombre)
    {
        _context.turnoAuditorias.Add(new TurnoAuditoria
        {
            TurnoId = turno.id,
            UsuarioNombre = usuarioNombre,
            MomentoAccion = DateTime.UtcNow,
            Accion = accion,
            FechaAnterior = turno.fecha,
            HoraAnterior = turno.hora,
            EstadoAnterior = _stateService.GetEstadoActual(turno).GetNombreEstado(),
            EstadoNuevo = estadoNuevo,
            PacienteId = turno.paciente_id.Value,
            PacienteNombre = turno.paciente.idNavigation.nombre,
            MedicoId = turno.medico_id.Value,
            MedicoNombre = turno.medico.idNavigation.nombre,
            EspecialidadId = turno.especialidad_id.Value,
            EspecialidadNombre = turno.especialidad.nombre,
            SlotIdAnterior = turno.slot_id
        });

        _context.trazabilidadTurnos.Add(new TrazabilidadTurno
        {
            TurnoId = turno.id,
            UsuarioNombre = usuarioNombre,
            MomentoAccion = DateTime.UtcNow,
            Accion = accion,
            Descripcion = descripcion,
            UsuarioRol = RolUsuario.System
        });
    }

    public async Task<Turno> GetTurnoActual(int paciente_id, int medico_id)
    {
        Turno turnoActual = await _context.turnos.Where(t => t.paciente_id == paciente_id && t.medico_id == medico_id && t.fecha.Value.ToDateTime(t.slot.horainicio.Value).AddMinutes(-5) < DateTime.Now  && DateTime.Now < t.fecha.Value.ToDateTime(t.slot.horafin.Value).AddMinutes(30)).FirstOrDefaultAsync();//sirve porque los turnos no pueden ser asignados de madrugada

        if(turnoActual == null) return null;
        
        return turnoActual;
    }
     
}