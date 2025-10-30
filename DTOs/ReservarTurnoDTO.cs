using MedCenter.Attributes;
using System.ComponentModel.DataAnnotations;
namespace MedCenter.DTOs;

public class ReservarTurnoDTO
{
    [Required]
    public int MedicoId { get; set; }

    [Required]
    public int EspecialidadId { get; set; }

    [Required]
    public int PacienteId { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    public int SlotId { get; set; } // ← Referencia al slot de agenda
}