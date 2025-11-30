using MedCenter.Models;

namespace MedCenter.Services.TurnoStates
{
    public static class TurnoStateFactory
    {
        public static ITurnoState CrearEstado(string nombreEstado)
        {
            return nombreEstado switch
            {
                "Disponible" => new TurnoDisponibleState(),
                "Reservado" => new TurnoReservadoState(),
                "Reprogramado" => new TurnoReprogramadoState(),
                "Cancelado" => new TurnoCanceladoState(),
                "Finalizado" => new TurnoFinalizadoState(),
                "Ausentado" => new TurnoAusentadoState(),
                _ => throw new ArgumentException($"Estado desconocido: {nombreEstado}")
            };
        }
    }
}