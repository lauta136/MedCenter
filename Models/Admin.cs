using MedCenter.Models;
using System.ComponentModel.DataAnnotations;

namespace MedCenter.Model
{
    public class Admin
    {
        public int Id {get;set;}
        
        [Required]
        [StringLength(50)]
        public string Cargo { get; set; } = string.Empty; // "Administrador General", "Administrador de Sistema", etc.
    
        public DateOnly Fecha_ingreso { get; set; }

        public bool Activo {get;set;}

        public virtual Persona IdNavigation {get;set;} = null!;
    }
}