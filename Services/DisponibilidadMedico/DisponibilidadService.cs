using MedCenter.Data;
using MedCenter.Models;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
namespace MedCenter.Services.DisponibilidadMedico
{
    public class DisponibilidadService
    {
        private AppDbContext _context;
        public DisponibilidadService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DateOnly>> GetDiasDisponibles(int id_medico) //sacar turnos de aca a un mes maximo
        {

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Now);

            DateOnly limite = DateOnly.FromDateTime(DateTime.Now.AddDays(30));

            var fechas = await _context.slotsagenda
                        .Where(sa => sa.medico_id == id_medico && sa.fecha!.Value >= hoy && sa.fecha.Value <= limite)
                        .Select(sa => sa.fecha!.Value)
                        .Distinct()
                        .OrderBy(f => f)
                        .ToListAsync();

            return fechas;

        }

        public async Task<List<SlotAgenda>> GetSlotsDisponibles(int id_medico, DateOnly fecha)
        {
            return await _context.slotsagenda.Where(sa => sa.medico_id == id_medico && sa.fecha == fecha && sa.disponible == true)
                                             .OrderBy(sa => sa.horainicio)
                                             .ToListAsync();
        }

        public async Task<bool> SlotEstaDisponible(int id_slotagenda)
        {
            var condition = await _context.slotsagenda.Where(sa => sa.id == id_slotagenda && sa.disponible == true).FirstOrDefaultAsync();

            if (condition == null) return false;

            return true;
        }

        public async Task OcuparSlot(int id_slotagenda)
        {
            var slot = await _context.slotsagenda.Where(sa => sa.id == id_slotagenda).FirstOrDefaultAsync();

            slot!.disponible = false;

            _context.Update(slot);
            await _context.SaveChangesAsync();
        }

        public async Task LiberarSlot(int id_slotagenda)
        {
            var slot = await _context.slotsagenda.Where(sa => sa.id == id_slotagenda).FirstOrDefaultAsync();

            slot!.disponible = true;

            _context.Update(slot);
            await _context.SaveChangesAsync();
        }
    }
}