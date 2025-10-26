using System.ComponentModel.DataAnnotations;

namespace MedCenter.DTOs;

public class ManipularDisponibilidadDTO
{
    [Required]
    [Range(0, 6)] // 0=Domingo, 1=Lunes, ..., 6=Sábado
    public DayOfWeek Dia_semana { get; set; }
    
    [Required]
    public TimeOnly Hora_inicio { get; set; }
    
    [Required]
    public TimeOnly Hora_fin { get; set; }

    public int Duracion_turno_minutos { get; set; }


}