using MedCenter.Models;
using MedCenter.Services.TurnoStates; // Add this if TurnoReservadoState is in this namespace
using MedCenter;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoDisponibleState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            turno.estado = "Reservado";
            return new TurnoReservadoState();
        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Reprogramar");
        }

        public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Cancelar");
        }

        public ITurnoState Finalizar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Finalizar");
        }

        public string GetColorBadge()
        {
            throw new NotImplementedException();
        }

        public string GetDescripcion() => "secondary";

        public string GetNombreEstado()
        {
            return "Disponible";
        }

        public bool PuedeCancelar() => false;

        public bool PuedeFinalizar() => false;

        public bool PuedeReprogramar() => false;

        public bool PuedeReservar() => true;


    }
}