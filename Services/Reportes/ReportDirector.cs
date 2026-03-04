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
    private Reporte? _lastReporte;

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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId, especialidadId);
        
        // Full report: all components included
        constructor.BuildHeader();
        await constructor.BuildData();        // ✅ Detailed data
        await constructor.BuildStatistics();  // ✅ Statistics
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId, especialidadId);
        
        // Summary report: skip detailed data, show only totals
        constructor.BuildHeader();
        // SKIP BuildData() - no detailed rows!
        await constructor.BuildStatistics();  // ✅ Only aggregates
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId, especialidadId);
        
        // Executive report: metrics first, data second
        constructor.BuildHeader();
        await constructor.BuildStatistics();  // ✅ Key metrics first
        await constructor.BuildData();        // ✅ Supporting data after
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId: medicoId);
        constructor.SetAuditFilters(usuarioNombre, accion, pacienteId);
        
        constructor.BuildHeader();
        await constructor.BuildData();
        await constructor.BuildStatistics();
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId, especialidadId);
        
        constructor.BuildHeader();
        await constructor.BuildData();
        await constructor.BuildStatistics();
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
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
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetLoginAuditFilters(usuarioNombre, tipoLogout, rol);
        
        constructor.BuildHeader();
        await constructor.BuildData();
        await constructor.BuildStatistics();
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
    }
    
    /// <summary>
    /// TRAZABILIDAD TURNOS Configuration - Shows the full traceability log of turn actions
    /// </summary>
    public async Task ConstructTrazabilidadTurnos(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        string? usuarioNombre = null,
        AccionesTurno? accion = null)
    {
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetAuditFilters(usuarioNombre, accion);

        constructor.BuildHeader();
        await constructor.BuildData();
        await constructor.BuildStatistics();
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
    }

    /// <summary>
    /// INTERACTIVE DASHBOARD Configuration - Produces data for Chart.js rendering
    /// </summary>
    public async Task ConstructDashboard(
        ReporteConstructor constructor,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? medicoId = null,
        int? especialidadId = null)
    {
        constructor.Reset();
        constructor.SetDateRange(fechaDesde, fechaHasta);
        constructor.SetFilters(medicoId, especialidadId);

        constructor.BuildHeader();
        await constructor.BuildData();
        await constructor.BuildStatistics();
        constructor.BuildFooter();
        constructor.BuildFormat();
        _lastReporte = constructor.Reporte;
    }

    /// <summary>
    /// Get the final constructed report
    /// </summary>
    public Reporte GetReporte()
    {
        if (_lastReporte == null)
        {
            throw new InvalidOperationException("No se ha construido ningún reporte. Llame a Construct() primero.");
        }
        
        return _lastReporte;
    }
}
