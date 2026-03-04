using MedCenter.DTOs;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Interactive Dashboard Report
/// Produces a Reporte whose Data part holds an EstadisticasAvanzadasDTO
/// for rendering as an interactive Chart.js page in the browser.
/// </summary>
public class ReporteDashboardConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private EstadisticasAvanzadasDTO? _stats;

    public ReporteDashboardConstructor(ReportesService reportesService) 
        : base(TipoReporte.DashboardInteractivo)
    {
        _reportesService = reportesService;
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Dashboard Interactivo - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        _stats ??= await _reportesService.CalcularEstadisticasAvanzadas(
            FechaDesde,
            FechaHasta,
            MedicoId,
            EspecialidadId
        );

        // Store the DTO directly — the controller will pass it to a Razor view
        Reporte[ParteReporte.Data] = _stats;
    }

    public override async Task BuildStatistics()
    {
        _stats ??= await _reportesService.CalcularEstadisticasAvanzadas(
            FechaDesde,
            FechaHasta,
            MedicoId,
            EspecialidadId
        );
        Reporte[ParteReporte.Statistics] = _stats;
    }

    public override void BuildFooter()
    {
        Reporte[ParteReporte.Footer] = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    public override void BuildFormat()
    {
        Reporte[ParteReporte.Format] = "HTML Interactivo (Chart.js)";
    }
}
