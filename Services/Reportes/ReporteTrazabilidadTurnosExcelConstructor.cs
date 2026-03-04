using MedCenter.DTOs;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Trazabilidad Turnos Excel Reports
/// </summary>
public class ReporteTrazabilidadTurnosExcelConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private List<TrazabilidadTurnoReporteDTO> _data;

    public ReporteTrazabilidadTurnosExcelConstructor(ReportesService reportesService)
        : base(TipoReporte.TrazabilidadTurnos)
    {
        _reportesService = reportesService;
        _data = new List<TrazabilidadTurnoReporteDTO>();
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Trazabilidad de Turnos - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        _data = await _reportesService.ObtenerTrazabilidadTurnos(
            FechaDesde,
            FechaHasta,
            UsuarioNombre,
            Accion
        );

        var excelBytes = _reportesService.GenerarExcelTrazabilidadTurnos(_data);
        Reporte[ParteReporte.Data] = excelBytes;
    }

    public override async Task BuildStatistics()
    {
        var stats = new
        {
            TotalRegistros = _data.Count,
            PorAccion = _data.GroupBy(d => d.Accion)
                             .ToDictionary(g => g.Key, g => g.Count()),
            PorRol = _data.GroupBy(d => d.UsuarioRol)
                          .ToDictionary(g => g.Key, g => g.Count()),
            PorUsuario = _data.GroupBy(d => d.UsuarioNombre)
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
