using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoFinalizadoState : ITurnoState
    {
        public string GetNombreEstado() => "Finalizado";
        public string GetDescripcion() => "Turno completado - paciente fue atendido";
        public string GetColorBadge() => "primary";

        public bool PuedeReservar(Turno turno) => false;
        public bool PuedeReprogramar(Turno turno) => false;
        public bool PuedeCancelar(Turno turno) => false;
        public bool PuedeFinalizar() => false;

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "reservar", false);
        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "reprogramar", false);
        }

        public ITurnoState Cancelar(Turno turno, string motivo)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "cancelar", false);
        }

        public ITurnoState Finalizar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "finalizar", false);
        }
    }
}