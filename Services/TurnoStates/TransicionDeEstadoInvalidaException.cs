
namespace MedCenter.Services.TurnoStates
{
    public class TransicionDeEstadoInvalidaException : Exception
    {
        public TransicionDeEstadoInvalidaException(string estadoActual, string accion)
               : base($"No se puede realizar la acción '{accion}' desde el estado '{estadoActual}'")
        {

        }
    }
}