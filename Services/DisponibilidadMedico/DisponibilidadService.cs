using MedCenter.Data;
using MedCenter.Models;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using MedCenter.DTOs;

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

        public async Task<DisponibilidadResult> AgregarBloqueDisponibilidad(int medico_id, ManipularDisponibilidadDTO dto)
        {

            if (await NuevaDisponibilidadCoherente(dto, medico_id))
            {
                _context.disponibilidad_medico.Add(new Models.DisponibilidadMedico
                {
                    medico_id = medico_id,
                    dia_semana = dto.Dia_semana,
                    hora_inicio = dto.Hora_inicio,
                    hora_fin = dto.Hora_fin,
                    vigencia_desde = DateOnly.FromDateTime(DateTime.Now),
                    vigencia_hasta = DateOnly.FromDateTime(DateTime.Now.AddDays(60)),
                    duracion_turno_minutos = dto.Duracion_turno_minutos
                });
                await _context.SaveChangesAsync();

                return new DisponibilidadResult { success = true, message = "El bloque fue agregado exitosamente" };

            }
            else
            {
                return new DisponibilidadResult { success = false, message = "El nuevo horario ingresado se superpone con uno ya activo" };
            }

        }


        /*public async Task<DisponibilidadResult> EditarBloqueDisponibilidad(ManipularDisponibilidadDTO dto, int dispo_id, int medico_id)
        {

            if (await NuevaDisponibilidadCoherente(dto, medico_id))
            {
                var dispo = await _context.disponibilidad_medico.FirstOrDefaultAsync(dm => dm.id == dispo_id);

                if (dispo == null) return new DisponibilidadResult { success = false, message = "El bloque de disponibilidad no fue encontrado" };

                dispo.dia_semana = dto.Dia_semana;
                dispo.hora_inicio = dto.Hora_inicio;
                dispo.hora_fin = dto.Hora_fin;
                dispo.duracion_turno_minutos = dto.Duracion_turno_minutos;

                _context.Update(dispo);
                await _context.SaveChangesAsync();

                return new DisponibilidadResult { success = true, message = "El bloque fue actualizado exitosamente" };
            }
            else
            {
                return new DisponibilidadResult { success = false, message = "El nuevo horario ingresado se superpone con uno ya activo" };
            }

        }
        */
        public async Task<DisponibilidadResult> CancelarBloqueDisponibilidad(int medico_id, ManipularDisponibilidadDTO dto, int dispo_id)
        {
        
            var dispo = await _context.disponibilidad_medico.FirstOrDefaultAsync(dm => dm.id == dispo_id);
            if (dispo == null) return new DisponibilidadResult { success = true, message = "No se encintri el bloque a editar" };

            dispo.activa = false;
            dispo.vigencia_hasta = DateOnly.FromDateTime(DateTime.Now);

            _context.Update(dispo);

            await _context.SaveChangesAsync();

            await _context.slotsagenda
            .Include(sa => sa.Turno)
            .Where(sa => sa.bloqueDisponibilidadId == dispo_id && sa.Turno == null).ExecuteDeleteAsync();

            return new DisponibilidadResult { success = true, message = "El bloque fue cancelado exitosamente"};
             
        }
        
         public async Task<bool> NuevaDisponibilidadCoherente(ManipularDisponibilidadDTO dto, int medico_id) //Que la nueva insercion no se superponga con otra ya guardada
        {
            // bool flag = true;

            if (dto.Hora_inicio >= dto.Hora_fin) return false;

            if ((dto.Hora_fin - dto.Hora_inicio).TotalMinutes < dto.Duracion_turno_minutos) return false; 

            var disponibilidadesPrecisas = await _context.disponibilidad_medico.Where(dm => dm.medico_id == medico_id && dm.activa == true && dm.dia_semana == dto.Dia_semana)
                                                                               .ToListAsync();

            foreach (var item in disponibilidadesPrecisas)
            {
                if (dto.Hora_inicio.IsBetween(item.hora_inicio, item.hora_fin) || dto.Hora_fin.IsBetween(item.hora_inicio, item.hora_fin))
                    return false;
            }

            return true;
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

        public async Task<DisponibilidadResult> GenerarSlotsAgenda(int medico_id)
        {
            var bloques = await _context.disponibilidad_medico.Where(dm => dm.medico_id == medico_id && dm.activa == true)
            .Include(dm => dm.slotsAgenda)
            .ToListAsync();

            if (bloques == null) return new DisponibilidadResult{ success = false, message = "El medico no tiene bloques de disponibilidad activos" };


            List<SlotAgenda> slotsAAgregar = new List<SlotAgenda>();

            foreach (var bloque in bloques)
            {
                
                var span = bloque.hora_fin - bloque.hora_inicio; //TimeOnly se convierte a TimeSpan, TimeSpan tiene la propiedad TotalMinutes,TimeOnly no la tiene , solo tiene Minutes que no es lo que queres, solo agarra la parte minutos, no convierte todo a minutos
                int cantSlots = (int)(span.TotalMinutes / bloque.duracion_turno_minutos);

                DateOnly fechaSlotInicio = DateOnly.FromDateTime(DateTime.Now);
                /*DateOnly Fecha = new DateOnly();
                TimeOnly Hora = new TimeOnly();
                */
                while (fechaSlotInicio.DayOfWeek != bloque.dia_semana) //cambiar logica, por ejemplo si genero los slots el martes a las 8 de la noche para un bloque de martes, se generan los slots de ese martes mas temprano, slots que nunca podran usarse
                {
                    fechaSlotInicio = fechaSlotInicio.AddDays(1);
                }

                for ( DateOnly Fecha = fechaSlotInicio; Fecha <= bloque.vigencia_hasta; Fecha = Fecha.AddDays(7))
                {
                    for ( TimeOnly Hora = bloque.hora_inicio; Hora.AddMinutes(bloque.duracion_turno_minutos) <= bloque.hora_fin; Hora = Hora.AddMinutes(bloque.duracion_turno_minutos))
                    {
                        if (!bloque.slotsAgenda.Any(sa => sa.disponible == true && sa.fecha == Fecha)) //problema aca, no se mete adentro
                        {
                            slotsAAgregar.Add(new SlotAgenda
                            {
                                fecha = Fecha,
                                horainicio = Hora,
                                horafin = Hora.AddMinutes(bloque.duracion_turno_minutos),
                                disponible = true,
                                medico_id = medico_id,
                                bloqueDisponibilidadId = bloque.id
                            });
                        }
                    }
                }
                
            }

            await _context.slotsagenda.AddRangeAsync(slotsAAgregar);
            await _context.SaveChangesAsync();
            return new DisponibilidadResult{ success = true, message = "Los slots se generaron exitosamente"};
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