using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MedCenter.Services.Reportes;
using MedCenter.Services.MedicoSv;
using MedCenter.Services.EspecialidadService;
using MedCenter.DTOs;
using MedCenter.Services.TurnoSv;
using DocumentFormat.OpenXml.Wordprocessing;
using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Office2016.Excel;
using MedCenter.Attributes;
using MedCenter.Services.AdminService;

namespace MedCenter.Controllers
{
    [Authorize]
    public class ReportesController : BaseController
    {
        private readonly ReportesService _reportesService;
        private readonly MedicoService _medicoService;
        private readonly EspecialidadService _especialidadService;
        private readonly ReportDirector _reportDirector;
        private readonly AppDbContext _context;
        private readonly AdminService _adminService;
        public ReportesController(ReportesService reportesService, MedicoService medicoService, EspecialidadService especialidadService, AppDbContext context, AdminService adminService)
        {
            _reportesService = reportesService;
            _medicoService = medicoService;
            _especialidadService = especialidadService;
            _reportDirector = new ReportDirector();
            _adminService = adminService;
            _context = context;
        }

        // GET: Reportes (View)
        [Authorize(Roles = $"{nameof(RolUsuario.Secretaria)},{nameof(RolUsuario.Medico)}")]
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

            var usuarios = await GetAllUsuarios();
            ViewBag.usuarios = usuarios;
            ViewBag.EsAdmin = await _adminService.EsAdmin(UserId.Value);

            if (User.IsInRole(RolUsuario.Medico.ToString()))
                return View("~/Views/Medico/Reportes.cshtml", medicos);
            else
                return View("~/Views/Secretaria/Reportes.cshtml", medicos);
        }

        // GET: Medicos por Especialidad (AJAX)
        [Authorize(Roles = $"{nameof(RolUsuario.Secretaria)},{nameof(RolUsuario.Medico)}")]
        [HttpGet]
        public async Task<IActionResult> GetMedicosPorEspecialidad(int especialidadId)
        {
            var medicos = await _especialidadService.GetMedicosPorEspecialidad(especialidadId);
            return Json(medicos);
        }

        [Authorize(Roles = $"{nameof(RolUsuario.Secretaria)}")]
        [HttpGet]
        public async Task<List<UsuarioDTO>> GetAllUsuarios()
        {
            var preUsuarios = await _context.personas.Select(p => new UsuarioDTO{Nombre = p.nombre,Id = p.id}).ToListAsync();

           //var usuarios = preUsuarios.Where(_context.medicos.AnyAsync(m => m.id == id))
            List<UsuarioDTO> usuarios = new List<UsuarioDTO>();
            foreach(var usuario in preUsuarios)
            {
                if(!await _context.medicos.AnyAsync(p => p.id == usuario.Id))
                {
                    usuario.Acciones.AddRange(AccionesTurno.INSERT.ToString(), AccionesTurno.CANCEL.ToString(), AccionesTurno.UPDATE.ToString());
                    usuarios.Add(usuario);
                }
            }
            
             usuarios.Add(new UsuarioDTO {
                Nombre = RolUsuario.System.ToString(),
                Id = -1, 
                Acciones = new List<string>
                {
                    AccionesTurno.FINALIZE.ToString(),
                    AccionesTurno.NOSHOW.ToString()
                }
            });
            return usuarios;
        }

        // ==== SECRETARIA REPORTS ====

        // Download PDF - Turnos por fecha
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_turnos_general")]
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
                
                // Director constructs DETAILED report (like constructSportsCar in Car example)
                await _reportDirector.ConstructDetailedReport(builder, desde, hasta);
                
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
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_turnos_general")]
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
                
                // Director constructs DETAILED report (same configuration, different builder)
                await _reportDirector.ConstructDetailedReport(builder, desde, hasta);
                
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

        // NEW: SUMMARY REPORT - Only statistics, no detailed data (like constructSUV in Car example)
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_turnos_resumen")]
        [HttpGet]
        public async Task<IActionResult> DescargarResumenTurnosPDF(string fechaDesde, string fechaHasta)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                var builder = new ReportePDFConstructor(_reportesService);
                
                // Different configuration: SUMMARY (no detailed rows)
                await _reportDirector.ConstructSummaryReport(builder, desde, hasta);
                
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Resumen_Turnos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // NEW: EXECUTIVE REPORT - Statistics first, then data (optimized for high-level overview)
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_ejecutivo")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteEjecutivoPDF(string fechaDesde, string fechaHasta)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                // Use EXECUTIVE builder with advanced statistics and charts
                var builder = new ReporteEjecutivoPDFConstructor(_reportesService);
                
                // Different configuration: EXECUTIVE (metrics first, with charts)
                await _reportDirector.ConstructExecutiveReport(builder, desde, hasta);
                
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Reporte_Ejecutivo_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [RequiredPermission("reporte:create_audit_turno")]
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [HttpGet]
        public async Task<IActionResult> DescargarAuditoriaTurnosPDF(string fechaDesde, string fechaHasta, string? usuarioNombre, AccionesTurno? accion, int? pacienteId, int? medicoId)
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
                var builder = new ReporteAuditoriaTurnosPDFConstructor(_reportesService);
                
                // Director constructs the audit report
                await _reportDirector.ConstructAuditoria(builder, desde, hasta, usuarioNombre, accion, pacienteId, medicoId);
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Auditoria_Turnos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

       //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_audit_turno")]
        [HttpGet]
        public async Task<IActionResult> DescargarReporteAuditoriaTurnosExcel(string fechaDesde, string fechaHasta, string? usuarioNombre, AccionesTurno? accion, int? pacienteId, int? medicoId)
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
                var builder = new ReporteAuditoriaTurnosExcelConstructor(_reportesService);
                
                // Director constructs the audit report
                await _reportDirector.ConstructAuditoria(builder, desde, hasta, usuarioNombre, accion, pacienteId, medicoId);
                
                // Get the final product
                var reporte = _reportDirector.GetReporte();
                var excelBytes = reporte.GetBytes();
                
                return File(
                    excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Auditoria_Turnos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Download PDF - Pacientes
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_pacientes_todos")]
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
        //[Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_pacientes_todos")]
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
        //[Authorize(Roles = $"{nameof(RolUsuario.Secretaria)},{nameof(RolUsuario.Medico)}")]
        [RequiredPermission("reporte:create_turnos_filtrado")]
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
            if (User.IsInRole(RolUsuario.Medico.ToString()) && !medicoId.HasValue)
                medicoId = UserId;

            try
            {
                // Create PDF builder
                var builder = new ReporteTurnosPorMedicoPDFConstructor(_reportesService);
                
                // Director constructs the turnos por medico report
                await _reportDirector.ConstructTurnosPorMedico(builder, desde, hasta, medicoId, especialidadId);
                
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
        //[Authorize(Roles = $"{nameof(RolUsuario.Secretaria)},{nameof(RolUsuario.Medico)}")]
        [RequiredPermission("reporte:create_turnos_filtrado")]
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
                var builder = new ReporteTurnosPorMedicoExcelConstructor(_reportesService);
                
                // Director constructs the turnos por medico report
                await _reportDirector.ConstructTurnosPorMedico(builder, desde, hasta, medicoId, especialidadId);
                
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
        //[Authorize(Roles = nameof(RolUsuario.Medico))]
        [RequiredPermission("reporte:create_entradas_creadas")]
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
        [Authorize(Roles = nameof(RolUsuario.Medico))]
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

        // Download PDF - Auditoría Logins
        [Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_audit_login")]
        [HttpGet]
        public async Task<IActionResult> DescargarAuditoriaLoginsPDF(
            string fechaDesde, 
            string fechaHasta, 
            string? usuarioNombre = null,
            string? tipoLogout = null,
            string? rol = null)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                var builder = new ReporteAuditoriaLoginsPDFConstructor(_reportesService);
                await _reportDirector.ConstructAuditoriaLogins(builder, desde, hasta, usuarioNombre, tipoLogout, rol);
                var reporte = _reportDirector.GetReporte();
                var pdfBytes = reporte.GetBytes();
                
                return File(pdfBytes, "application/pdf", $"Auditoria_Logins_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Download Excel - Auditoría Logins
        [Authorize(Roles = nameof(RolUsuario.Secretaria))]
        [RequiredPermission("reporte:create_audit_login")]
        [HttpGet]
        public async Task<IActionResult> DescargarAuditoriaLoginsExcel(
            string fechaDesde, 
            string fechaHasta, 
            string? usuarioNombre = null,
            string? tipoLogout = null,
            string? rol = null)
        {
            if (!DateTime.TryParse(fechaDesde, out var desde) || 
                !DateTime.TryParse(fechaHasta, out var hasta))
            {
                TempData["ErrorMessage"] = "Fechas inválidas";
                return RedirectToAction("Index");
            }

            try
            {
                var builder = new ReporteAuditoriaLoginsExcelConstructor(_reportesService);
                await _reportDirector.ConstructAuditoriaLogins(builder, desde, hasta, usuarioNombre, tipoLogout, rol);
                var reporte = _reportDirector.GetReporte();
                var excelBytes = reporte.GetBytes();
                
                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Auditoria_Logins_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al generar reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
