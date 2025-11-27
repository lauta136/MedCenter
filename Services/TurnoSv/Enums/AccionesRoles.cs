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

public enum FuturosEstadosTurno
{
    Ausentado,
    Cancelado,
    Reservado,
    Reprogramado,
    Finalizado
}