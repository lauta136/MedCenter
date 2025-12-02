namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'Builder' abstract class in Builder Pattern
/// Defines the steps to construct a report
/// Subclasses implement these steps to create different report formats
/// </summary>
public abstract class ReporteConstructor
{
    // Filter properties that can be set before construction
    protected DateTime FechaDesde { get; private set; }
    protected DateTime FechaHasta { get; private set; }
    protected int? MedicoId { get; private set; }
    protected int? EspecialidadId { get; private set; }
    protected string? UserRole { get; private set; }
    protected int? UserId { get; private set; }
    
    /// <summary>
    /// The product being built
    /// </summary>
    public Reporte Reporte { get; protected set; }
    
    protected ReporteConstructor(TipoReporte tipoReporte)
    {
        Reporte = new Reporte(tipoReporte);
    }
    
    /// <summary>
    /// Set date range for the report
    /// </summary>
    public void SetDateRange(DateTime desde, DateTime hasta)
    {
        FechaDesde = desde;
        FechaHasta = hasta;
    }
    
    /// <summary>
    /// Set filter parameters (optional)
    /// </summary>
    public void SetFilters(int? medicoId = null, int? especialidadId = null)
    {
        MedicoId = medicoId;
        EspecialidadId = especialidadId;
    }
    
    /// <summary>
    /// Set user context for role-based filtering
    /// </summary>
    public void SetUserContext(string role, int? userId)
    {
        UserRole = role;
        UserId = userId;
    }
    
    // Abstract construction steps - each builder implements these differently
    public abstract void BuildHeader();
    public abstract void BuildData();
    public abstract void BuildStatistics();
    public abstract void BuildFooter();
    public abstract void BuildFormat();
}