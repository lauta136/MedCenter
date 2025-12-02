using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MedCenter.Services.Reportes;
using MedCenter.Services.MedicoSv;
using MedCenter.Services.EspecialidadService;
using MedCenter.DTOs;
using MedCenter.Services.TurnoSv;

namespace MedCenter.Controllers
{
    [Authorize]
    public class ReportesController : BaseController
    {
        private readonly ReportesService _reportesService;
        private readonly MedicoService _medicoService;
        private readonly EspecialidadService _especialidadService;
        private readonly ReportDirector _reportDirector;
        
        public ReportesController(ReportesService reportesService, MedicoService medicoService, EspecialidadService especialidadService)
        {
            _reportesService = reportesService;
            _medicoService = medicoService;
            _especialidadService = especialidadService;
            _reportDirector = new ReportDirector();
        }

        // GET: Reportes (View)
        [Authorize(Roles = "Secretaria,Medico")]
        public async Task<IActionResult> Index()
        {
            ViewBag.UserName = UserName;
            
            var estadisticas = await _reportesService.ObtenerEstadisticasMes(
                DateTime.Now.Month, 
                DateTime.Now.Year,
                User.IsInRole("Medico") ? UserId : null
            );

            ViewBag.TurnosMes = estadisticas.TurnosMes;
            ViewBag.TurnosCancelados = estadisticas.TurnosCancelados;
            ViewBag.PacientesNuevosMes = estadisticas.PacientesTotales;
            ViewBag.PacientesAtendidos = estadisticas.PacientesAtendidos;

            List<MedicoViewDTO> medicos = await _medicoService.GetAll();
            var especialidades = await _especialidadService.GetEspecialidadesCargadas();
            ViewBag.Especialidades = especialidades;

            if (User.IsInRole(RolUsuario.Medico.ToString()))
                return View("~/Views/Medico/Reportes.cshtml", medicos);
            else
                return View("~/Views/Secretaria/Reportes.cshtml", medicos);
        }

        // GET: Medicos por Especialidad (AJAX)
        [Authorize(Roles = "Secretaria,Medico")]
        [HttpGet]
        public async Task<IActionResult> GetMedicosPorEspecialidad(int especialidadId)
        {
            var medicos = await _especialidadService.GetMedicosPorEspecialidad(especialidadId);
            return Json(medicos);
        }

        // ==== SECRETARIA REPORTS ====

        // Download PDF - Turnos por fecha
        [Authorize(Roles = "Secretaria")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteTurnosPDF(string fechaDesde, string fechaHasta)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                // Create PDF builder
                var builder = new ReportePDFConstructor(_reportesService);
                
                // Director constructs the report using the builder
                _reportDirector.Construct(builder, desde, hasta);
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Turnos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Download Excel - Turnos por fecha
        [Authorize(Roles = "Secretaria")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteTurnosExcel(string fechaDesde, string fechaHasta)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                // Create Excel builder
                var builder = new ReporteExcelConstructor(_reportesService);
                
                // Director constructs the report using the builder
                _reportDirector.Construct(builder, desde, hasta);
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var excelBytes = reporte.GetBytes();
                
                return File(
                    excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Turnos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Download PDF - Pacientes
        [Authorize(Roles = "Secretaria")]
        [HttpGet]
        public async Task<IActionResult> DescargarPacientesPDF()
        {
            var pacientes = await _reportesService.ObtenerTodosPacientes();
            
            if (!pacientes.Any())
            {
                TempData["ErrorMessage"] = "No hay pacientes registrados";
                return RedirectToAction("Index");
            }

            var pdfBytes = _reportesService.GenerarPDFPacientes(pacientes);

            return File(pdfBytes, "application/pdf", $"Pacientes_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Download Excel - Pacientes
        [Authorize(Roles = "Secretaria")]
        [HttpGet]
        public async Task<IActionResult> DescargarPacientesExcel()
        {
            var pacientes = await _reportesService.ObtenerTodosPacientes();
            
            if (!pacientes.Any())
            {
                TempData["ErrorMessage"] = "No hay pacientes registrados";
                return RedirectToAction("Index");
            }

            var excelBytes = _reportesService.GenerarExcelPacientes(pacientes);

            return File(
                excelBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Pacientes_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }

        // ==== MEDICO REPORTS ====

        // Download PDF - Médico específico
        [Authorize(Roles = "Secretaria,Medico")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteMedicoPDF(int? medicoId, int? especialidadId, string fechaDesde, string fechaHasta)
        {
            // Parse dates
            DateTime desde, hasta;
            
            if (!string.IsNullOrEmpty(fechaDesde) && !string.IsNullOrEmpty(fechaHasta))
            {
                if (!DateTime.TryParse(fechaDesde, out desde) || 
                    !DateTime.TryParse(fechaHasta, out hasta))
                {
                    TempData["ErrorMessage"] = "Fechas inválidas";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Default to last month
                hasta = DateTime.Now;
                desde = hasta.AddMonths(-1);
            }

            // If medico role, use their own ID
            if (User.IsInRole("Medico") && !medicoId.HasValue)
                medicoId = UserId;

            try
            {
                // Create PDF builder
                var builder = new ReportePDFConstructor(_reportesService);
                
                // Director constructs the report with filters
                _reportDirector.Construct(
                    builder, 
                    desde, 
                    hasta,
                    medicoId,
                    especialidadId,
                    User.IsInRole("Medico") ? "Medico" : "Secretaria",
                    UserId
                );
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Reporte_Turnos_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Download Excel - Médico específico
        [Authorize(Roles = "Secretaria,Medico")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteMedicoExcel(int? medicoId, int? especialidadId, string fechaDesde, string fechaHasta)
        {
            // Parse dates
            DateTime desde, hasta;
            
            if (!string.IsNullOrEmpty(fechaDesde) && !string.IsNullOrEmpty(fechaHasta))
            {
                if (!DateTime.TryParse(fechaDesde, out desde) || 
                    !DateTime.TryParse(fechaHasta, out hasta))
                {
                    TempData["ErrorMessage"] = "Fechas inválidas";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Default to last month
                hasta = DateTime.Now;
                desde = hasta.AddMonths(-1);
            }

            // If medico role, use their own ID
            if (User.IsInRole("Medico") && !medicoId.HasValue)
                medicoId = UserId;

            try
            {
                // Create Excel builder
                var builder = new ReporteExcelConstructor(_reportesService);
                
                // Director constructs the report with filters
                _reportDirector.Construct(
                    builder, 
                    desde, 
                    hasta,
                    medicoId,
                    especialidadId,
                    User.IsInRole("Medico") ? "Medico" : "Secretaria",
                    UserId
                );
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var excelBytes = reporte.GetBytes();
                
                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Reporte_Turnos_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ==== HISTORIAS CLINICAS REPORTS ====

        // Download PDF - Historias Clínicas
        [Authorize(Roles = "Medico")]
        [HttpGet]
        public async Task<IActionResult> DescargarHistoriasClinicasPDF(string fechaDesde, string fechaHasta)
        {
            if (!DateOnly.TryParse(fechaDesde, out var desde) || 
                !DateOnly.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            var entradas = await _reportesService.ObtenerEntradasClinicas(desde, hasta, UserId!.Value);

            if (!entradas.Any())
            {
                TempData["ErrorMessage"] = "No hay entradas clínicas en el período seleccionado";
                return RedirectToAction("Index");
            }

            var pdfBytes = _reportesService.GenerarPDFHistoriasClinicas(
                entradas,
                $"Historias Clínicas - {desde:dd/MM/yyyy} a {hasta:dd/MM/yyyy}"
            );

            return File(pdfBytes, "application/pdf", $"Historias_Clinicas_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Download Excel - Historias Clínicas
        [Authorize(Roles = "Medico")]
        [HttpGet]
        public async Task<IActionResult> DescargarHistoriasClinicasExcel(string fechaDesde, string fechaHasta)
        {
            if (!DateOnly.TryParse(fechaDesde, out var desde) || 
                !DateOnly.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            var entradas = await _reportesService.ObtenerEntradasClinicas(desde, hasta, UserId!.Value);

            if (!entradas.Any())
            {
                TempData["ErrorMessage"] = "No hay entradas clínicas en el período seleccionado";
                return RedirectToAction("Index");
            }

            var excelBytes = _reportesService.GenerarExcelHistoriasClinicas(entradas, "Historias Clínicas");

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Historias_Clinicas_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
