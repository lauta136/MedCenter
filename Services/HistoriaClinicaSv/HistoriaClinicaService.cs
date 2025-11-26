using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    public async Task<HistoriaClinica> GetHistoriaClinicaPaciente(int id) //creo que no usado, revisar
    {
        
        return await _context.historiasclinicas.Include(hc => hc.EntradasClinicas).ThenInclude(ec => ec.medico)
            .Include(hc => hc.EntradasClinicas).ThenInclude(ec => ec.turno).Where(hc => hc.paciente_id == id).FirstOrDefaultAsync();
    }

    public async Task<List<HistoriaClinicaViewDTO>> GetTodasHistoriasClinicas(int medico_id)
    {
        List<HistoriaClinicaViewDTO> historias = new List<HistoriaClinicaViewDTO>();
        var pacientes = await _context.pacientes.Include(p => p.idNavigation)
                                .Include(p => p.turnos)
                                .ThenInclude(t => t.medico).ThenInclude(m => m.idNavigation)
                                .Include(p => p.historiasclinicas)
                                .ThenInclude(hc => hc.EntradasClinicas)
                                .Where(p => p.turnos.Any(t => t.medico_id == medico_id)).ToListAsync();

        foreach(Paciente paciente in pacientes)
        {
            if(paciente.historiasclinicas != null) 
            {
                List<EntradaClinicaViewDTO> entradas = new List<EntradaClinicaViewDTO>();
                foreach(EntradaClinica entrada in paciente.historiasclinicas.EntradasClinicas)
                {
                    entradas.Add(new EntradaClinicaViewDTO
                    {   
                        Id = entrada.id,
                        Fecha = entrada.fecha,
                        MedicoNombre = entrada.medico.idNavigation.nombre,
                        Diagnostico = entrada.diagnostico,
                        Tratamiento = entrada.tratamiento,
                        Observaciones = entrada.observaciones
                    });
                }
                historias.Add(new HistoriaClinicaViewDTO 
                {
                    PacienteId = paciente.id, 
                    PacienteNombre = paciente.idNavigation.nombre,
                    PacienteDni = paciente.dni,
                    PacienteEmail = paciente.idNavigation.email,
                    PacienteTelefono = paciente.telefono,
                    HistoriaId = paciente.historiasclinicas.id,
                    Entradas = entradas
                });

            };
        }
        return historias;
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

        if(EstaEnTurno(turno.slot.horainicio.Value, turno.slot.horafin.Value, turno.fecha.Value))
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

    public bool EstaEnTurno(TimeOnly horaInicio, TimeOnly horaFin, DateOnly fecha)
    {
        DateTime tiempoPermitidoFin = fecha.ToDateTime(horaFin).AddMinutes(30);
        DateTime tiempoPermitidoInicio = fecha.ToDateTime(horaInicio).AddMinutes(-5);

        if(DateTime.Now > tiempoPermitidoInicio && DateTime.Now < tiempoPermitidoFin)
        return true;

        return false;
    }

    public async Task<HistoriaServiceResult> CrearHistoriaClinica(int paciente_id)
    {
        if(paciente_id == 0) return new HistoriaServiceResult{success = false, message = "Error en identificacion de paciente"};

        bool tiene = await _context.historiasclinicas.AnyAsync(hc => hc.paciente_id == paciente_id);

        if(tiene) return new HistoriaServiceResult{success = false, message = "El paciente ya tiene una historia clinica"};

        _context.historiasclinicas.Add(new HistoriaClinica{paciente_id = paciente_id});
        await _context.SaveChangesAsync();
        return new HistoriaServiceResult{success = true, message = "La historia clinica fue creada con exito"};
    }
    
    public async Task<HistoriaClinicaViewDTO> VerHistoriaClinica(int paciente_id)
    {
        Paciente paciente = await _context.pacientes.Include(p => p.idNavigation)
                                  .Include(p => p.historiasclinicas).ThenInclude(hc => hc.EntradasClinicas)
                                  .ThenInclude(ec => ec.medico)
                                  .ThenInclude(m => m.idNavigation)
                                  .Where(p => p.id == paciente_id).FirstOrDefaultAsync();

        if(paciente.historiasclinicas != null) 
        {
            List<EntradaClinicaViewDTO> entradas = new List<EntradaClinicaViewDTO>();
            foreach(EntradaClinica entrada in paciente.historiasclinicas.EntradasClinicas)
            {
                entradas.Add(new EntradaClinicaViewDTO
                {   Id = entrada.id,
                    Fecha = entrada.fecha,
                    MedicoNombre = entrada.medico.idNavigation.nombre,
                    Diagnostico = entrada.diagnostico,
                    Tratamiento = entrada.tratamiento,
                    Observaciones = entrada.observaciones
                });
            }
            return new HistoriaClinicaViewDTO 
            {
                PacienteId = paciente.id, 
                PacienteNombre = paciente.idNavigation.nombre,
                PacienteDni = paciente.dni,
                PacienteEmail = paciente.idNavigation.email,
                PacienteTelefono = paciente.telefono,
                HistoriaId = paciente.historiasclinicas.id,
                Entradas = entradas
            };

        };
        return new HistoriaClinicaViewDTO 
            {
                PacienteId = paciente.id, 
                PacienteNombre = paciente.idNavigation.nombre,
                PacienteDni = paciente.dni,
                PacienteEmail = paciente.idNavigation.email,
                PacienteTelefono = paciente.telefono,
            };
    }
}
