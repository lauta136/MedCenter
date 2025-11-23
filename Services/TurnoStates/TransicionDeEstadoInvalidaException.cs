
namespace MedCenter.Services.TurnoStates
{
    public class TransicionDeEstadoInvalidaException : Exception
    {
        public TransicionDeEstadoInvalidaException(string estadoActual, string accion, bool fecha)
               : base($"No se puede realizar la acción '{accion}' desde el estado '{estadoActual}'" + (fecha ?  "debido a que hay menos de 24 hs de antelacion" : ""))
        {
            
        }
    }
}