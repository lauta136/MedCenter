using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Services.Reportes;

/// <summary>
/// Concrete Builder for Login Audit PDF Reports
/// </summary>
public class ReporteAuditoriaLoginsPDFConstructor : ReporteConstructor
{
    private readonly ReportesService _reportesService;
    private List<LoginAuditoriaReporteDTO> _loginData;

    public ReporteAuditoriaLoginsPDFConstructor(ReportesService reportesService) 
        : base(TipoReporte.AuditoriaLogins)
    {
        _reportesService = reportesService;
        _loginData = new List<LoginAuditoriaReporteDTO>();
    }

    public override void BuildHeader()
    {
        Reporte[ParteReporte.Header] = $"Registro de Auditoría de Login/Logout - {FechaDesde:dd/MM/yyyy} a {FechaHasta:dd/MM/yyyy}";
    }

    public override async Task BuildData()
    {
        // Parse TipoLogout from string if provided
        TipoLogout? tipoLogout = null;
        if (!string.IsNullOrEmpty(TipoLogoutStr) && Enum.TryParse<TipoLogout>(TipoLogoutStr, out var tipo))
        {
            tipoLogout = tipo;
        }

        // Parse RolUsuario from string if provided
        RolUsuario? rol = null;
        if (!string.IsNullOrEmpty(RolStr) && Enum.TryParse<RolUsuario>(RolStr, out var rolParsed))
        {
            rol = rolParsed;
        }

        _loginData = await _reportesService.ObtenerAuditoriaLogins(
            FechaDesde, 
            FechaHasta, 
            UsuarioNombre, 
            tipoLogout, 
            rol
        );
        
        var pdfBytes = _reportesService.GenerarPDFAuditoriaLogins(_loginData);
        Reporte[ParteReporte.Data] = pdfBytes;
    }

    public override async Task BuildStatistics()
    {
        var stats = new
        {
            TotalSesiones = _loginData.Count,
            SesionesActivas = _loginData.Count(l => !l.MomentoLogout.HasValue),
            SesionesCerradas = _loginData.Count(l => l.MomentoLogout.HasValue),
            PorRol = _loginData.GroupBy(l => l.UsuarioRol).ToDictionary(g => g.Key, g => g.Count()),
            PorTipoLogout = _loginData.Where(l => l.TipoLogout != null)
                                       .GroupBy(l => l.TipoLogout!)
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
