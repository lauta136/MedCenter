using System.ComponentModel.DataAnnotations;

namespace MedCenter.DTOs;

public class EntradaClinicaRegisterDTO
{
    [Required]
    public string diagnostico { get; set; }

    [Required]
    public string tratamiento { get; set; }

    [Required]
    public string? observaciones { get; set; } 
}