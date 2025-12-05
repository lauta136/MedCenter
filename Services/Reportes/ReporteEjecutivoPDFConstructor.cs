using MedCenter.DTOs;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Executive Report with Advanced Statistics and Charts
/// Cumple requisito: genera gráficos y estadísticas que asisten en toma de decisiones
/// </summary>
public class ReporteEjecutivoPDFConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private EstadisticasAvanzadasDTO _stats;

    public ReporteEjecutivoPDFConstructor(ReportesService reportesService) 
        : base(TipoReporte.TurnosSecretaria)
    {
        _reportesService = reportesService;
        _stats = new EstadisticasAvanzadasDTO();
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Reporte Ejecutivo - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        // Calculate advanced statistics FIRST (needed for PDF generation)
        _stats = await _reportesService.CalcularEstadisticasAvanzadas(
            FechaDesde,
            FechaHasta,
            MedicoId,
            EspecialidadId
        );

        // Generate executive PDF with statistics and charts
        var pdfBytes = _reportesService.GenerarPDFEstadisticasAvanzadas(
            _stats,
            "Reporte Ejecutivo de Gestión",
            FechaDesde,
            FechaHasta
        );

        Reporte[ParteReporte.Data] = pdfBytes;
    }

    public override async Task BuildStatistics()
    {
        // Statistics are already embedded in the PDF
        // But we store them separately for potential API access
        Reporte[ParteReporte.Statistics] = _stats;
        await Task.CompletedTask;
    }

    public override void BuildFooter()
    {
        Reporte[ParteReporte.Footer] = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    public override void BuildFormat()
    {
        Reporte[ParteReporte.Format] = "PDF con Gráficos y KPIs";
    }
}
