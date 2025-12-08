using MedCenter.Models;
using MedCenter.Services.TurnoSv;

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
            turno.estado = EstadosTurno.Cancelado.ToString();
            turno.motivo_cancelacion = motivo_cancelacion;
            return new TurnoCanceladoState();
        }

        public ITurnoState Finalizar(Turno turno)
        {
            turno.estado = EstadosTurno.Finalizado.ToString();
            return new TurnoFinalizadoState();
        }

        public string GetNombreEstado() => EstadosTurno.Reprogramado.ToString();
        public string GetDescripcion() => "Turno reprogramado (no puede volver a reprogramarse)";
        public string GetColorBadge() => "warning";

        public bool PuedeReservar(Turno turno) => false;
        public bool PuedeReprogramar(Turno turno) => false; // ⭐ REGLA DE NEGOCIO: Solo se reprograma UNA vez
        public bool PuedeCancelar(Turno turno) => true;
        public bool PuedeFinalizar() => true;

        public ITurnoState Ausentar(Turno turno)
        {
            turno.estado = EstadosTurno.Ausentado.ToString();
            return new TurnoAusentadoState();
        }

        public bool PuedeMarcarAusente(Turno turno)
        {
            return true;
        }
    }
}