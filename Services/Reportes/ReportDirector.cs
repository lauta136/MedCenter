using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'Director' class in Builder Pattern
/// Orchestrates the construction process using different configurations.
/// Similar to the Car example: Director defines HOW to build (configuration),
/// while Builders define WHAT to produce (PDF vs Excel).
/// </summary>
public class ReportDirector
{
    private ReporteConstructor? _constructor;
    
    /// <summary>
    /// DETAILED REPORT Configuration - Full data with all statistics
    /// Like constructSportsCar() in the Car example - a specific configuration
    /// </summary>
    public async Task ConstructDetailedReport(
        ReporteConstructor constructor, 
        DateTime fechaDesde, 
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId, especialidadId);
        
        // Full report: all components included
        _constructor.BuildHeader();
        await _constructor.BuildData();        // ✅ Detailed data
        await _constructor.BuildStatistics();  // ✅ Statistics
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// SUMMARY REPORT Configuration - Only aggregated statistics, no detailed data
    /// Like constructSUV() in the Car example - different configuration
    /// </summary>
    public async Task ConstructSummaryReport(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId, especialidadId);
        
        // Summary report: skip detailed data, show only totals
        _constructor.BuildHeader();
        // SKIP BuildData() - no detailed rows!
        await _constructor.BuildStatistics();  // ✅ Only aggregates
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// EXECUTIVE REPORT Configuration - Statistics first, then supporting data
    /// Optimized for high-level overview
    /// </summary>
    public async Task ConstructExecutiveReport(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId, especialidadId);
        
        // Executive report: metrics first, data second
        _constructor.BuildHeader();
        await _constructor.BuildStatistics();  // ✅ Key metrics first
        await _constructor.BuildData();        // ✅ Supporting data after
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// AUDIT REPORT Configuration - Specialized for auditoría with audit-specific filters
    /// </summary>
    public async Task ConstructAuditoria(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        string? usuarioNombre = null,
        AccionesTurno? accion = null,
        int? pacienteId = null,
        int? medicoId = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId: medicoId);
        _constructor.SetAuditFilters(usuarioNombre, accion, pacienteId);
        
        _constructor.BuildHeader();
        await _constructor.BuildData();
        await _constructor.BuildStatistics();
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// TURNOS POR MEDICO Configuration - Specialized for doctor schedule reports
    /// </summary>
    public async Task ConstructTurnosPorMedico(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetFilters(medicoId, especialidadId);
        
        _constructor.BuildHeader();
        await _constructor.BuildData();
        await _constructor.BuildStatistics();
        _constructor.BuildFooter();
        _constructor.BuildFormat();
    }
    
    /// <summary>
    /// LOGIN AUDIT Configuration - Specialized for login/logout tracking
    /// </summary>
    public async Task ConstructAuditoriaLogins(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        string? usuarioNombre = null,
        string? tipoLogout = null,
        string? rol = null)
    {
        _constructor = constructor;
        _constructor.SetDateRange(fechaDesde, fechaHasta);
        _constructor.SetLoginAuditFilters(usuarioNombre, tipoLogout, rol);
        
        _constructor.BuildHeader();
        await _constructor.BuildData();
        await _constructor.BuildStatistics();
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
