using MedCenter.Models;

namespace MedCenter.Services.TurnoStates;

public class TurnoAusentadoState : ITurnoState
{
    public ITurnoState Ausentar(Turno turno)
    {
        throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"ausentar", false);
    }

    public ITurnoState Cancelar(Turno turno, string motivo_cancelacion)
    {
        throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"cancelar", false);
    }

    public ITurnoState Finalizar(Turno turno)
    {
        throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"finalizar", false);
    }

    public string GetColorBadge() => "dark";

    public string GetDescripcion()
    {
        return "El paciente se ausento sin cancelacion previa";
    }

    public string GetNombreEstado()
    {
        return "Ausente";
    }

    public bool PuedeAusentar(Turno turno)
    {
        return false;
    }

    public bool PuedeCancelar(Turno turno)
    {
        return false;
    }

    public bool PuedeFinalizar()
    {
        return false;
    }

    public bool PuedeReprogramar(Turno turno)
    {
        return false;
    }

    public bool PuedeReservar(Turno turno)
    {
        return false;
    }

    public ITurnoState Reprogramar(Turno turno)
    {
        throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"Reprogramar",false);
    }

    public ITurnoState Reservar(Turno turno)
    {
        throw new TransicionDeEstadoInvalidaException(GetNombreEstado(),"Reservar",false);
    }
}