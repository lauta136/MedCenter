using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public class TurnoStateService
    {
        public ITurnoState GetEstadoActual(Turno turno)
        {
            return TurnoStateFactory.CrearEstado(turno.estado ?? "Disponible");
        }

        public void Reservar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            var nuevoEstado = estado.Reservar(turno);
            // El estado del turno ya fue modificado dentro del método
        }

        public void Reprogramar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            var nuevoEstado = estado.Reprogramar(turno);
        }

        public void Cancelar(Turno turno, string motivo)
        {
            var estado = GetEstadoActual(turno);
            var nuevoEstado = estado.Cancelar(turno, motivo);
        }

        public void Finalizar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            var nuevoEstado = estado.Finalizar(turno);
        }

        public void Ausentar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            var nuevoEstado = estado.Ausentar(turno);
        }

        public bool PuedeReprogramar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            return estado.PuedeReprogramar(turno);
        }

        public bool PuedeCancelar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            return estado.PuedeCancelar(turno);
        }

        public bool PuedeFinalizar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            return estado.PuedeFinalizar();
        }

        public bool PuedeAusentar(Turno turno)
        {
            var estado = GetEstadoActual(turno);
            return estado.PuedeFinalizar();
        }
    }
}