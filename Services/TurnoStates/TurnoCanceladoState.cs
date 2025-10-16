using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{

    public class TurnoCanceladoState : ITurnoState

    {
        public string GetNombreEstado() => "Cancelado";
        public string GetDescripcion() => "Turno cancelado - estado final";
        public string GetColorBadge() => "danger";

        public bool PuedeReservar() => false;
        public bool PuedeReprogramar() => false;
        public bool PuedeCancelar() => false;
        public bool PuedeFinalizar() => false;

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "reservar");
        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "reprogramar");
        }

        public ITurnoState Cancelar(Turno turno, string motivo)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "cancelar");
        }

        public ITurnoState Finalizar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "finalizar");
        }
    }
}