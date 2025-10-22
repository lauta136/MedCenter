using MedCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Models;
using MedCenter.DTOs;

namespace MedCenter.Services.EspecialidadService;

public class EspecialidadService
{
    // Agregar este método a tu TurnoController o crear un EspecialidadController
    private readonly AppDbContext _context;

    public EspecialidadService(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<List<Especialidad>> GetEspecialidades()
    {
        var especialidades = await _context.especialidades
                                          .ToListAsync();
        return especialidades;
    }

    [HttpGet]
    public async Task<List<MedicoViewDTO>> GetMedicosPorEspecialidad(int especialidadId)
    {
        var medicos = await _context.medicoEspecialidades
                                           .Where(me => me.especialidad.id == especialidadId && me.medico != null)
                                           .Include(me => me.medico) // Incluimos los datos de la especialidad
                                           .ThenInclude(me => me.idNavigation)
                                           .Select(me => new MedicoViewDTO{ Nombre = me.medico.idNavigation.nombre, Id = me.medico.id, Matricula = me.medico.matricula })
                                           .ToListAsync();

        return medicos;
    }
    
    public async Task<List<Especialidad>> GetEspecialidadesCargadas() //especialidades que tienen asociado algun medico que esta cargado en el sistema 
    {
        var especialidades = await _context.especialidades.ToListAsync();

        return especialidades;
    }

}