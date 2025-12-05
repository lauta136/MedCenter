using MedCenter.DTOs;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Turnos por Médico PDF Reports
/// </summary>
public class ReporteTurnosPorMedicoPDFConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private List<TurnoReporteDTO> _turnosData;

    public ReporteTurnosPorMedicoPDFConstructor(ReportesService reportesService) 
        : base(TipoReporte.TurnosPorMedico)
    {
        _reportesService = reportesService;
        _turnosData = new List<TurnoReporteDTO>();
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Turnos por Médico - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        _turnosData = await _reportesService.ObtenerTurnosPorFecha(
            DateOnly.FromDateTime(FechaDesde), 
            DateOnly.FromDateTime(FechaHasta), 
            MedicoId, 
            EspecialidadId
        );
        
        var pdfBytes = _reportesService.GenerarPDFTurnos(_turnosData, "Turnos por Médico");
        Reporte[ParteReporte.Data] = pdfBytes;
    }

    public override async Task BuildStatistics()
    {
        var stats = new
        {
            TotalTurnos = _turnosData.Count,
            PorEstado = _turnosData.Where(t => !string.IsNullOrEmpty(t.Estado))
                                   .GroupBy(t => t.Estado)
                                   .ToDictionary(g => g.Key, g => g.Count()),
            PorMedico = _turnosData.GroupBy(t => $"{t.MedicoNombre} {t.MedicoApellido}")
                                   .ToDictionary(g => g.Key, g => g.Count())
        };
        Reporte[ParteReporte.Statistics] = stats;
        await Task.CompletedTask;
    }

    public override void BuildFooter()
    {
        Reporte[ParteReporte.Footer] = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    public override void BuildFormat()
    {
        Reporte[ParteReporte.Format] = "PDF";
    }
}
