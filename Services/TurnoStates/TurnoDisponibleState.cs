using MedCenter.Models;
using MedCenter.Services.TurnoStates; // Add this if TurnoReservadoState is in this namespace
using MedCenter;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoDisponibleState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            if(turno.fecha!.Value > DateOnly.FromDateTime(DateTime.Now.AddHours(24)))
            {
                turno.estado = "Reservado";
                return new TurnoReservadoState();
            }
            else throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"Reservar", true);

        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Reprogramar", false);
        }

        public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Cancelar", false);
        }

        public ITurnoState Finalizar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(), "Finalizar", false);
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

        public bool PuedeCancelar(Turno turno) => false;

        public bool PuedeFinalizar() => false;

        public bool PuedeReprogramar(Turno turno) => false;

        public bool PuedeReservar(Turno turno) => turno.fecha > DateOnly.FromDateTime(DateTime.Now.AddHours(24));


    }
}