using MedCenter.DTOs;
using MedCenter.Models;

public interface ITurnoState
{
    string GetNombreEstado();

    ITurnoState Reservar(Turno turno);
    ITurnoState Cancelar(Turno turno, string motivo_cancelacion);
    ITurnoState Reprogramar(Turno turno);
    ITurnoState Finalizar(Turno turno);
    ITurnoState Ausentar(Turno turno);

    bool PuedeReprogramar(Turno turno);
    bool PuedeFinalizar();
    bool PuedeReservar(Turno turno);
    bool PuedeCancelar(Turno turno);
    bool PuedeAusentar(Turno turno);

    string GetDescripcion();
    string GetColorBadge();
}