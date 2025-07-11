namespace MedCenter.Models;

public class MedicoEspecialidad
{
    public int medicoId { get; set; }
    public virtual Medico medico { get; set; } = null!;

    public int especialidadId { get; set; }
    public virtual Especialidad especialidad { get; set; } = null!;
}
