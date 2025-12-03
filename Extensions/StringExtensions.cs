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
}