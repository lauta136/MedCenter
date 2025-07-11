namespace MedCenter.Models;

public class MedicoEspecialidad
{
    public int medicoId { get; set; }
    public virtual Medicos medico { get; set; } = null!;

    public int especialidadId { get; set; }
    public virtual Especialidades especialidad { get; set; } = null!;
}
