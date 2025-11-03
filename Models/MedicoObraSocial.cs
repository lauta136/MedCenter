namespace MedCenter.Models;

public class MedicoObraSocial
{
    public int id { get; set; } // PK surrogate
    public int medico_id { get; set; }
    public int obrasocial_id { get; set; }
    
    public DateOnly fecha_desde { get; set; }
    public DateOnly? fecha_hasta { get; set; }
    public bool activo { get; set; } //seteado por defecto a true en DbContext
    public  Medico medico { get; set; } = null!;
    public  ObraSocial obrasocial { get; set; } = null!;
}