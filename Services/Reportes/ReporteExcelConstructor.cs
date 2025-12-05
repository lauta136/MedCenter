namespace MedCenter.Services.Reportes;

/// <summary>
/// The 'ConcreteBuilder' class for Excel Turnos Reports
/// Implements all construction steps to build an Excel report
/// </summary>
public class ReporteExcelConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    
    public ReporteExcelConstructor(ReportesService reportesService) 
        : base(TipoReporte.TurnosSecretaria)
    {
        _reportesService = reportesService;
    }
    
    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = "Reporte de Turnos - Excel";
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
        
        // Generate Excel
        var excelBytes = _reportesService.GenerarExcelTurnos(
            turnos, 
            "Reporte de Turnos"
        );
        
        Reporte[ParteReporte.Data] = excelBytes;
    }
    
    public override async Task BuildStatistics()
    {
        // Excel doesn't support summary-only reports for now
        Reporte[ParteReporte.Statistics] = "Incluye estadísticas en Excel";
        await Task.CompletedTask;
    }
    
    public override void BuildFooter()
    {
        Reporte[ParteReporte.Footer] = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }
    
    public override void BuildFormat()
    {
        Reporte[ParteReporte.Format] = "Excel";
    }
}
