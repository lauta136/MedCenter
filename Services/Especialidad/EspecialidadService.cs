using MedCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedCenter.Models;
using MedCenter.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MedCenter.Services.EspecialidadService;

public class EspecialidadService
{
    // Agregar este método a tu TurnoController o crear un EspecialidadController
    private readonly AppDbContext _context;

    public EspecialidadService(AppDbContext context)
    {
        _context = context;
    }


    public async Task<List<Especialidad>> GetEspecialidades()
    {
        var especialidades = await _context.especialidades
                                          .ToListAsync();
        return especialidades;
    }


    public async Task<List<MedicoViewDTO>> GetMedicosPorEspecialidad(int especialidadId)
    { //ver que hacer con el filtro de activo por como se usa este metodo para los reportes
      //se usa para ver que turnos fueron con esa especialidad y podes elegir un medico (lista que provee este metodo filtrando por especialidad)
      //que despues se usa como respuesta JSON en controladora (en el JS de la pagina),  
        var medicos = await _context.medicoEspecialidades
                                           .Where(me => me.especialidad.id == especialidadId && me.medico != null && me.activo == true)
                                           .Include(me => me.medico) // Incluimos los datos de la especialidad
                                           .ThenInclude(me => me.idNavigation)
                                           .Select(me => new MedicoViewDTO{ Nombre = me.medico.idNavigation.nombre, Id = me.medico.id, Matricula = me.medico.matricula , Activo = me.medico.idNavigation.activo})
                                           .ToListAsync();

        return medicos;
    }
    //para mi hacer otro metodo para el reporte en donde me fijo si el medico (que no tenga la especialidad activa)
    //tiene algun turno con esa especialidad en el ultimo año, ahi si lo mando (muestro) sino no.


    public async Task<List<MedicoViewDTO>> GetMedicosPorEspecialidadReporte(int? especialidadId, int mesesAtras)
    {   
        var limite = DateOnly.FromDateTime(DateTime.Now).AddMonths(-mesesAtras);

        if(!especialidadId.HasValue)
        { //que solo devuelva cuentas inactivas cuando tienen turnos en el periodo
            var todos =  await _context.medicos
            .Include(me => me.idNavigation).Select(me => new MedicoViewDTO{ Nombre = me.idNavigation.nombre, Id = me.id, Matricula = me.matricula , Activo = me.idNavigation.activo})
            .ToListAsync();

            var turnos = await _context.turnos.Where(t => t.fecha.Value.CompareTo(limite) >= 0).Select(t => t.medico_id.Value).ToHashSetAsync();

            var definitiveList1 = new List<MedicoViewDTO>();

            foreach(var medico in todos)
            {
                if(!medico.Activo)
                {
                    //var result = await _context.turnos.Where(t => t.medico_id == medico.Id && t.fecha.Value.CompareTo(limite) >= 0).AnyAsync();
                    if(turnos.Contains(medico.Id))
                    {
                        definitiveList1.Add(medico);
                    }
                }
                else
                definitiveList1.Add(medico);
            }

            return definitiveList1;
        }

        var medicos = await _context.medicoEspecialidades
                                           .Where(me => me.especialidad.id == especialidadId && me.medico != null)
                                           .Include(me => me.medico) // Incluimos los datos de la especialidad
                                           .ThenInclude(me => me.idNavigation)
                                           .Select(me => new { Nombre = me.medico.idNavigation.nombre, Id = me.medico.id, Matricula = me.medico.matricula , Activo = me.medico.idNavigation.activo, EspecilidadActiva = me.activo, EspecialidadId = me.especialidadId})
                                           .ToListAsync();

        var turnosConEspecialidad = await _context.turnos.Where(t => t.fecha.Value.CompareTo(limite) >= 0).Select(t => new {MedicoId = t.medico_id.Value, EspecialidadId = t.especialidad_id.Value}).ToHashSetAsync();

       
        var definitiveList = new List<MedicoViewDTO>();
        foreach(var medico in medicos)
        {
            if(!medico.EspecilidadActiva || !medico.Activo)
            {   
                //var result = await _context.turnos.Where(t => t.medico_id == medico.Id && t.especialidad_id == medico.EspecialidadId && t.fecha.Value.CompareTo(limite) >= 0).AnyAsync();

                if(turnosConEspecialidad.Contains(new {MedicoId = medico.Id, EspecialidadId = medico.EspecialidadId}))
                definitiveList.Add(new MedicoViewDTO{Nombre = medico.Nombre, Id = medico.Id, Matricula = medico.Matricula, Activo = medico.Activo});
            }
            else
            definitiveList.Add(new MedicoViewDTO{Nombre = medico.Nombre, Id = medico.Id, Matricula = medico.Matricula, Activo = medico.Activo});
        }

        return definitiveList;
    } 
    public async Task<List<Especialidad>> GetEspecialidadesCargadas() //especialidades que tienen asociado algun medico que esta cargado en el sistema 
    {
        var especialidades = await _context.especialidades.ToListAsync();

        return especialidades;
    }

}