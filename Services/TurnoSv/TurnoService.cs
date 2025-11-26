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

namespace MedCenter.Services.TurnoSv;

public class TurnoService
{
    private readonly AppDbContext _context;
    private readonly TurnoStateService _stateService;

    public TurnoService(AppDbContext appDbContext, TurnoStateService turnoStateService)
    {
        _context = appDbContext;
        _stateService = turnoStateService;
    }
    public async Task FinalizarTurnosPasados()
    {
        // Use the fully-qualified model type to avoid the Turno namespace/type conflict
        // and compare only by date here (time comparison omitted because the property was missing in the original expression).
       
        List<Turno> turnosAFinalizar =await  _context.turnos.Include(t => t.slot). Include(t => t.medico).ThenInclude(m => m.idNavigation)
                                                  .Include(t => t.paciente).ThenInclude(p => p.idNavigation)
                                                  .Include(t => t.especialidad)
                                                  .Where(t => t.slot != null && t.slot.disponible == false && t.fecha.Value.ToDateTime(t.slot.horafin.Value) < DateTime.Now
                                                   && (t.estado == "Reservado"|| t.estado == "Reprogramado"))
                                                  .ToListAsync();
        

        foreach(Turno turno in turnosAFinalizar)
        {
            

            _context.turnoAuditorias.Add(new TurnoAuditoria
            {
                TurnoId = turno.id,
                UsuarioNombre = "System",
                MomentoAccion = DateTime.UtcNow,
                Accion = "FINALIZE",
                FechaAnterior = turno.fecha,
                FechaNueva = null,
                HoraAnterior = turno.hora,
                HoraNueva = null,
                EstadoAnterior = turno.estado,
                EstadoNuevo = "Finalizado",
                PacienteId = turno.paciente_id.Value,
                PacienteNombre = turno.paciente.idNavigation.nombre,
                MedicoId = turno.medico_id.Value,
                MedicoNombre = turno.medico.idNavigation.nombre,
                EspecialidadId = turno.especialidad_id.Value,
                EspecialidadNombre = turno.especialidad.nombre,
                SlotIdAnterior = turno.slot_id,
                SlotIdNuevo = null,

            });
            _context.trazabilidadTurnos.Add(new TrazabilidadTurno
            {
                TurnoId = turno.id,
                UsuarioId = null,
                UsuarioRol = null,
                UsuarioNombre = "System",
                MomentoAccion = DateTime.UtcNow,
                Accion = "FINALIZE",
                Descripcion = $"El turno ha finalizado"
            });

            _stateService.Finalizar(turno);
        }

        await _context.SaveChangesAsync();

    }

    public async Task<Turno> GetTurnoActual(int paciente_id, int medico_id)
    {
        Turno turnoActual = await _context.turnos.Where(t => t.paciente_id == paciente_id && t.medico_id == medico_id && t.fecha.Value.ToDateTime(t.slot.horainicio.Value).AddMinutes(-5) < DateTime.Now  && DateTime.Now < t.fecha.Value.ToDateTime(t.slot.horafin.Value).AddMinutes(30)).FirstOrDefaultAsync();//sirve porque los turnos no pueden ser asignados de madrugada

        if(turnoActual == null) return null;
        
        return turnoActual;
    }
     
}