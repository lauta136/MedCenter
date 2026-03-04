namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'ConcreteBuilder' class for PDF Turnos Reports
/// Implements all construction steps to build a PDF report
/// </summary>
public class ReportePDFConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    
    public ReportePDFConstructor(ReportesService reportesService) 
        : base(TipoReporte.TurnosSecretaria)
    {
        _reportesService = reportesService;
    }
    
    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = "Reporte de Turnos - PDF";
    }
    
    public override async Task BuildData()
    {
        // Get data based on filters and user role
        var turnos = await _reportesService.ObtenerTurnosPorFecha(
            DateOnly.FromDateTime(FechaDesde), 
            DateOnly.FromDateTime(FechaHasta), 
            MedicoId, 
            EspecialidadId
        );
        
        // Generate PDF
        var pdfBytes = _reportesService.GenerarPDFTurnos(
            turnos, 
            "Reporte de Turnos"
        );
        
        Reporte[ParteReporte.Data] = pdfBytes;
    }
    
    public override async Task BuildStatistics()
    {
        // Always produces a standalone statistics PDF.
        // Reporte.GetBytes() returns Data when present, Statistics otherwise —
        // so this serves as the sole output for Summary reports and as supplemental
        // metadata for Detailed reports, without inspecting sibling build steps.
        var stats = await _reportesService.ObtenerEstadisticasMes(
            FechaDesde.Month,
            FechaDesde.Year,
            MedicoId
        );

        var statsPdfBytes = _reportesService.GenerarPDFEstadisticasSimples(
            stats,
            $"Resumen Estadístico de Turnos - {FechaDesde:MMMM yyyy}",
            FechaDesde,
            FechaHasta
        );
        Reporte[ParteReporte.Statistics] = statsPdfBytes;
    }
    
    public override void BuildFooter()
    {
        Reporte[ParteReporte.Footer] = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }
    
    public override void BuildFormat()
    {
        Reporte[ParteReporte.Format] = "PDF";
    }
}