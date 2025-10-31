public class TurnoAuditoria
{
    public int Id { get; set; }
    public int TurnoId { get; set; }
    public string UsuarioNombre { get; set; }
    public DateTime MomentoAccion { get; set; }
    public string Accion { get; set; } // INSERT, UPDATE, DELETE (soft)
    
    // ========================================
    // CAMPOS EDITABLES (tienen anterior/nuevo)
    // ========================================
    public DateOnly? FechaAnterior { get; set; }
    public DateOnly? FechaNueva { get; set; }
    
    public TimeOnly? HoraAnterior { get; set; }
    public TimeOnly? HoraNueva { get; set; }
    
    public string? EstadoAnterior { get; set; }
    public string? EstadoNuevo { get; set; }
    
    
    // ========================================
    // CAMPOS INMUTABLES (solo guardas UNA vez)
    // ========================================
    // Se guardan solo en INSERT para tener contexto
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; }
    
    public int MedicoId { get; set; }
    public string MedicoNombre { get; set; }
    
    public int EspecialidadId { get; set; }
    public string EspecialidadNombre { get; set; }
    
    public int? SlotIdAnterior { get; set; }
    public int? SlotIdNuevo { get; set; }
    
    public string? MotivoCancelacion { get; set; }

    public int? PacienteObraSocialId { get; set; }
    public string? ObraSocialNombre { get; set; }
}