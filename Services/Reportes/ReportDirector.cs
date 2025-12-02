namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'Director' class in Builder Pattern
/// Orchestrates the construction process
/// Constructs and assembles reports using the Builder interface
/// </summary>
public class ReportDirector
{
    private ReporteConstructor? _constructor;
    
    /// <summary>
    /// Director uses a complex series of steps to construct the report
    /// The same construction process creates different representations
    /// </summary>
    public void Construct(
        ReporteConstructor reporteConstructor, 
        DateTime fechaDesde, 
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null,
        string? userRole = null,
        int? userId = null)
    {
        _constructor = reporteConstructor;
        
        // Set parameters before construction
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId, especialidadId);
        
        if (!string.IsNullOrEmpty(userRole))
        {
            _constructor.SetUserContext(userRole, userId);
        }
        
        // Execute construction steps in specific order
        // The builder implements each step differently based on format (PDF/Excel)
        _constructor.BuildHeader();
        _constructor.BuildData();
        _constructor.BuildStatistics();
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// Get the final constructed report
    /// </summary>
    public Reporte GetReporte()
    {
        if (_constructor == null)
        {
            throw new InvalidOperationException("No se ha construido ningún reporte. Llame a Construct() primero.");
        }
        
        return _constructor.Reporte;
    }
}
