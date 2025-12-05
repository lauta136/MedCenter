using MedCenter.DTOs;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Auditoría de Turnos Excel Reports
/// </summary>
public class ReporteAuditoriaTurnosExcelConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private List<TurnoAuditoriaReporteDTO> _auditData;

    public ReporteAuditoriaTurnosExcelConstructor(ReportesService reportesService) 
        : base(TipoReporte.AuditoriaTurnos)
    {
        _reportesService = reportesService;
        _auditData = new List<TurnoAuditoriaReporteDTO>();
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Registro de Auditoría de Turnos - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        _auditData = await _reportesService.ObtenerAuditoriaTurnos(
            FechaDesde, 
            FechaHasta, 
            UsuarioNombre, 
            Accion, 
            PacienteId, 
            MedicoId
        );
        
        var excelBytes = _reportesService.GenerarExcelAuditoriaTurnos(_auditData);
        Reporte[ParteReporte.Data] = excelBytes;
    }

    public override async Task BuildStatistics()
    {
        var stats = new
        {
            TotalRegistros = _auditData.Count,
            PorAccion = _auditData.Where(a => a.EstadoActual.HasValue)
                                   .GroupBy(a => a.EstadoActual!.Value)
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
        Reporte[ParteReporte.Format] = "Excel";
    }
}
