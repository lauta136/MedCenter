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
    
    public override void BuildData()
    {
        // Get data based on filters and user role
        var turnos = _reportesService.ObtenerTurnosPorFecha(
            DateOnly.FromDateTime(FechaDesde), 
            DateOnly.FromDateTime(FechaHasta), 
            MedicoId, 
            EspecialidadId
        ).Result;
        
        // Generate Excel
        var excelBytes = _reportesService.GenerarExcelTurnos(
            turnos, 
            "Reporte de Turnos"
        );
        
        Reporte[ParteReporte.Data] = excelBytes;
    }
    
    public override void BuildStatistics()
    {
        Reporte[ParteReporte.Statistics] = "Incluye estadísticas en Excel";
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
