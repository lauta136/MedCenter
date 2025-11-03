using MedCenter.DTOs;
using MedCenter.Models;

public interface ITurnoState
{
    string GetNombreEstado();

    ITurnoState Reservar(Turno turno);
    ITurnoState Cancelar(Turno turno, string motivo_cancelacion);
    ITurnoState Reprogramar(Turno turno);
    ITurnoState Finalizar(Turno turno);

    bool PuedeReprogramar();
    bool PuedeFinalizar();
    bool PuedeReservar();
    bool PuedeCancelar();

    string GetDescripcion();
    string GetColorBadge();
}