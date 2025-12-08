using MedCenter.Models;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.TurnoStates
{
    public static class TurnoStateFactory
    {
        public static ITurnoState CrearEstado(string nombreEstado)
        {
            return nombreEstado switch
            {
                var s when s == EstadosTurno.Disponible.ToString() => new TurnoDisponibleState(),
                var s when s == EstadosTurno.Reservado.ToString() => new TurnoReservadoState(),
                var s when s == EstadosTurno.Reprogramado.ToString() => new TurnoReprogramadoState(),
                var s when s == EstadosTurno.Cancelado.ToString() => new TurnoCanceladoState(),
                var s when s == EstadosTurno.Finalizado.ToString() => new TurnoFinalizadoState(),
                var s when s == EstadosTurno.Ausentado.ToString() => new TurnoAusentadoState(),
                _ => throw new ArgumentException($"Estado desconocido: {nombreEstado}")
            };
        }
    }
}