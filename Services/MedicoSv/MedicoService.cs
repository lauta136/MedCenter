using MedCenter.Data;
using MedCenter.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.MedicoSv;

public class MedicoService
{   
    private readonly AppDbContext _context;

    public MedicoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task <List<MedicoViewDTO>> GetAll()
    {
        var medicos = await _context.medicos
            .Include(m => m.idNavigation)
             .Include(m => m.medicoEspecialidades)
                 .ThenInclude(me => me.especialidad)
             .Select(m => new MedicoViewDTO
             {
                Id = m.id,
                Nombre = m.idNavigation.nombre,
                Email = m.idNavigation.email,
                Matricula = m.matricula,
                Especialidades = m.medicoEspecialidades
                    .Select(me => me.especialidad.nombre ?? "Sin nombre")
                    .ToList()
             })
            .ToListAsync();

            return medicos;
        }

}