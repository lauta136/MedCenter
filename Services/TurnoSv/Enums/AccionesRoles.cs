namespace MedCenter.Services.TurnoSv;
public enum RolUsuario
{
    System,
    Paciente,
    Medico,
    Secretaria
}

public enum AccionesTurno
{
    FINALIZE,
    NOSHOW,
    UPDATE,
    INSERT,
}

public enum EstadosTurno
{
    Ausentado,
    Cancelado,
    Reservado,
    Reprogramado,
    Finalizado
}