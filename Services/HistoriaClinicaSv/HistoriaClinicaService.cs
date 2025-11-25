using System.Security.Cryptography;
using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.HistoriaClinicaSv;

public class HistoriaClinicaService
{
    private readonly AppDbContext _context;

    public HistoriaClinicaService(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }
    public async Task<List<HistoriaClinica>> GetHistoriaClinicaPaciente(int id)
    {
        
        return await _context.historiasclinicas.Include(hc => hc.EntradasClinicas).ThenInclude(ec => ec.medico)
            .Include(hc => hc.EntradasClinicas).ThenInclude(ec => ec.turno).Where(hc => hc.paciente_id == id).ToListAsync();
    }

    

    public async Task<HistoriaServiceResult> GuardarNuevaEntrada(EntradaClinicaRegisterDTO entradaDTO, int turno_id) //al usar poner un if si el serviceResult.success es verdadero
    {
       
        var turno = await _context.turnos.Where(t => t.id== turno_id).FirstOrDefaultAsync();

        if(turno == null) return new HistoriaServiceResult {success = false, message = "No se encontro el turno asociado"};

        if(turno.estado == "Cancelado") return new HistoriaServiceResult {success = false, message = "El turno no fue llevado a cabo"};

        if(turno.entradaClinica_id != null) return new HistoriaServiceResult{success = false, message = "El turno ya tiene una entrada clinica asociada"};

        await _context.Entry(turno).Reference(t => t.paciente).LoadAsync();
        await _context.Entry(turno.paciente).Reference(p => p.historiasclinicas).LoadAsync();
        await _context.Entry(turno).Reference(t => t.slot).LoadAsync();

        if(turno.paciente.historiasclinicas == null) return new HistoriaServiceResult {success = false, message = "El paciente no tiene historia clinica"};

       
        DateTime tiempoPermitidoFin = turno.fecha.Value.ToDateTime(turno.slot.horafin.Value).AddMinutes(30);
        DateTime tiempoPermitidoInicio = turno.fecha.Value.ToDateTime(turno.slot.horainicio.Value).AddMinutes(-5);

        if(DateTime.Now > tiempoPermitidoInicio && DateTime.Now < tiempoPermitidoFin)
        {
            EntradaClinica entrada = new EntradaClinica
            {
                observaciones = entradaDTO.observaciones, 
                diagnostico = entradaDTO.diagnostico, 
                fecha = DateOnly.FromDateTime(DateTime.Now), 
                tratamiento = entradaDTO.tratamiento, turno = turno, 
                medico_id = turno.medico_id.Value, 
                historia_id = turno.paciente.historiasclinicas.id 
            };

            _context.entradasclinicas.Add(entrada);
            await _context.SaveChangesAsync();

            return new HistoriaServiceResult {success = true, message = "La entrada fue registrada con exito"};
        }

        return new HistoriaServiceResult {success = false, message = "Debe estar atendiendo al paciente para guardar una nueva entrada clinica"};


    }

    public async Task<HistoriaServiceResult> CrearHistoriaClinica(int paciente_id)
    {
        bool tiene = await _context.historiasclinicas.AnyAsync(hc => hc.paciente_id == paciente_id);

        if(tiene) return new HistoriaServiceResult{success = false, message = "El paciente ya tiene una historia clinica"};

        _context.historiasclinicas.Add(new HistoriaClinica{paciente_id = paciente_id});
        await _context.SaveChangesAsync();
        return new HistoriaServiceResult{success = true, message = "La historia clinica fue creada con exito"};
    }
    
}