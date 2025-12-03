using System.ComponentModel.DataAnnotations;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Models;

public class TrazabilidadTurno
{
    public int Id { get; set; }
    [Required]
    public int TurnoId { get; set; }

    public int? UsuarioId { get; set; }
    [Required]
    public string UsuarioNombre { get; set; }

    public RolUsuario UsuarioRol { get; set; }
    [Required]
    public DateTime MomentoAccion { get; set; }
    [Required]
    [MaxLength(50)]
    public string Accion { get; set; }
    [Required]
    [MaxLength(200)]
    public string Descripcion { get; set; } //por que se hizo la accion
}