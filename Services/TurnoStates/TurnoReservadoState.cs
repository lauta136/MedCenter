using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoReservadoState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"reservar", false);
        }
        public ITurnoState Reprogramar(Turno turno)
        {
            turno.estado = "Reprogramado";
            return new TurnoReprogramadoState();
        }

        public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
        {
            if(turno.fecha!.Value > DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
            {
                turno.estado = "Cancelado";
                turno.motivo_cancelacion = motivo_cancelacion;
                return new TurnoCanceladoState();
            }
            else throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"cancelar", true);
        }

        public ITurnoState Finalizar(Turno turno)
        {
            turno.estado = "Finalizado";
            return new TurnoFinalizadoState();
        }

        public string GetColorBadge() => "success";
        public string GetDescripcion() => "Turno confirmado y pendiente de atención";

        public string GetNombreEstado() => "Reservado";

        public bool PuedeCancelar(Turno turno) => turno.fecha > DateOnly.FromDateTime(DateTime.Now.AddHours(24));

        public bool PuedeFinalizar() => true;

        public bool PuedeReprogramar(Turno turno) => turno.fecha > DateOnly.FromDateTime(DateTime.Now.AddHours(24));

        public bool PuedeReservar(Turno turno) => false;

        public ITurnoState Ausentar(Turno turno)
        {
            turno.estado = "Ausentado";
            return new TurnoAusentadoState();
        }

        public bool PuedeMarcarAusente(Turno turno)
        {
            return true;
        }
    }
}