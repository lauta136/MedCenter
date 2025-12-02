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
    
    public override void BuildData()
    {
        // Get data based on filters and user role
        var turnos = _reportesService.ObtenerTurnosPorFecha(
            DateOnly.FromDateTime(FechaDesde), 
            DateOnly.FromDateTime(FechaHasta), 
            MedicoId, 
            EspecialidadId
        ).Result;
        
        // Generate PDF
        var pdfBytes = _reportesService.GenerarPDFTurnos(
            turnos, 
            "Reporte de Turnos"
        );
        
        Reporte[ParteReporte.Data] = pdfBytes;
    }
    
    public override void BuildStatistics()
    {
        Reporte[ParteReporte.Statistics] = "Incluye estadísticas en PDF";
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