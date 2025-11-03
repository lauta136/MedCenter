using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoReservadoState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"Reservar");
        }
        public ITurnoState Reprogramar(Turno turno)
        {
            turno.estado = "Reprogramado";
            return new TurnoReprogramadoState();
        }

        public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
        {
            turno.estado = "Cancelado";
            turno.motivo_cancelacion = motivo_cancelacion;
            return new TurnoCanceladoState();
        }

        public ITurnoState Finalizar(Turno turno)
        {
            turno.estado = "Finalizado";
            return new TurnoFinalizadoState();
        }

        public string GetColorBadge() => "success";
        public string GetDescripcion() => "Turno confirmado y pendiente de atención";

        public string GetNombreEstado() => "Reservado";

        public bool PuedeCancelar() => true;

        public bool PuedeFinalizar() => true;

        public bool PuedeReprogramar() => true;

        public bool PuedeReservar() => false;

       
    }
}