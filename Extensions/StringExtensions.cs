using MedCenter.Services.TurnoSv;

namespace MedCenter.Extensions;

public static class StringExtensions
{
    public static RolUsuario ToRolUsuario(this string str)
    {
        if (Enum.TryParse<RolUsuario>(str, true, out var rol))
        {
            return rol;
        }

        throw new ArgumentException($"Invalid RolUsuario value: '{str}'", nameof(str));
    }

    public static EstadosTurno ToEstadoTurno(this string str)
    {
        if(Enum.TryParse<EstadosTurno>(str, true, out EstadosTurno estado))
        {
            return estado;
        }
        throw new ArgumentException($"Invalid EstadosTurno value: '{str}'", nameof(str));
    }
}