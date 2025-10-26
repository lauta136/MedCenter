// Models/DisponibilidadMedico.cs
using System.ComponentModel.DataAnnotations;

namespace MedCenter.Models;

public partial class DisponibilidadMedico //cada bloque de trabajo, puede haber 2 en el mismo dia del mismo medico en caso de que trabaje en momentos diferenetes y no de corrido
{
    public int id { get; set; }
    
    [Required]
    public int medico_id { get; set; }
    
    [Required]
    [Range(0, 6)] // 0=Domingo, 1=Lunes, ..., 6=Sábado
    public DayOfWeek dia_semana { get; set; }
    
    [Required]
    public TimeOnly hora_inicio { get; set; }
    
    [Required]
    public TimeOnly hora_fin { get; set; }
    
    
    [Range(5, 120)]
    public int duracion_turno_minutos { get; set; } //predefinido en 30 en context
    
    [Required]
    public DateOnly vigencia_desde { get; set; }
    
    public DateOnly? vigencia_hasta { get; set; }
    
    public bool? activa { get; set; } //predefinida true en context, el ? es crucial ya que sino, al agregar un nuevo DisponibilidadMedico, se pone activa como false, ya que bool no acepta null en c# y su valor predefinido es false
    
    // Navigation
    public Medico medico { get; set; } = null!;
}