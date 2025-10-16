using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoReprogramadoState : ITurnoState
    {

        public ITurnoState Reservar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"reservar");
        }

        public ITurnoState Reprogramar(Turno turno)
        {
            throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"reprogramar - el turno ya fue reprogramado previamente");
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

        public string GetNombreEstado() => "Reprogramado";
        public string GetDescripcion() => "Turno reprogramado (no puede volver a reprogramarse)";
        public string GetColorBadge() => "warning";

        public bool PuedeReservar() => false;
        public bool PuedeReprogramar() => false; // ⭐ REGLA DE NEGOCIO: Solo se reprograma UNA vez
        public bool PuedeCancelar() => true;
        public bool PuedeFinalizar() => true;

       
    }
}