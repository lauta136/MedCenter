using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoReprogramadoState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"reservar", false);
        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"reprogramar - el turno ya fue reprogramado previamente", false);
        }


        public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
        {
            if(turno.fecha!.Value > DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
            {
                turno.estado = "Cancelado";
                turno.motivo_cancelacion = motivo_cancelacion;
                return new TurnoCanceladoState();
            }
            else throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"Cancelar", true);

        }

        public ITurnoState Finalizar(Turno turno)
        {
            turno.estado = "Finalizado";
            return new TurnoFinalizadoState();
        }

        public string GetNombreEstado() => "Reprogramado";
        public string GetDescripcion() => "Turno reprogramado (no puede volver a reprogramarse)";
        public string GetColorBadge() => "warning";

        public bool PuedeReservar(Turno turno) => false;
        public bool PuedeReprogramar(Turno turno) => false; // ⭐ REGLA DE NEGOCIO: Solo se reprograma UNA vez
        public bool PuedeCancelar(Turno turno) => turno.fecha > DateOnly.FromDateTime(DateTime.Now.AddHours(24));
        public bool PuedeFinalizar() => true;

       
    }
}