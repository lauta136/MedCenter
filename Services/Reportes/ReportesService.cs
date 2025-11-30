using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using MedCenter.Data;
using MedCenter.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.Reportes
{
    public class ReportesService
    {
        private readonly AppDbContext _context;

        public ReportesService(AppDbContext context)
        {
            _context = context;
        }

        // ===== DATA RETRIEVAL METHODS =====

        public async Task<List<TurnoReporteDTO>> ObtenerTurnosPorFecha(DateOnly fechaDesde, DateOnly fechaHasta, int? medicoId = null, int? especialidadId = null)
        {
            var query = _context.turnos
                .Include(t => t.paciente)
                    .ThenInclude(p => p!.idNavigation)
                .Include(t => t.medico)
                    .ThenInclude(m => m!.idNavigation)
                .Include(t => t.especialidad)
                .Where(t => t.fecha >= fechaDesde && t.fecha <= fechaHasta);

            if (medicoId.HasValue)
                query = query.Where(t => t.medico_id == medicoId);

            if (especialidadId.HasValue)
                query = query.Where(t => t.especialidad_id == especialidadId);

            var turnos = await query
                .OrderBy(t => t.fecha)
                .ThenBy(t => t.hora)
                .ToListAsync();

            return turnos.Select(t => {
                var nombrePaciente = t.paciente!.idNavigation!.nombre;
                var partesPaciente = nombrePaciente.Split(' ', 2);
                var nombreMedico = t.medico!.idNavigation!.nombre;
                var partesMedico = nombreMedico.Split(' ', 2);
                
                return new TurnoReporteDTO
                {
                    Fecha = t.fecha!.Value.ToString("dd/MM/yyyy"),
                    Hora = t.hora!.Value.ToString(@"HH\:mm"),
                    PacienteApellido = partesPaciente.Length > 1 ? partesPaciente[1] : "",
                    PacienteNombre = partesPaciente[0],
                    PacienteDNI = t.paciente.dni!,
                    MedicoApellido = partesMedico.Length > 1 ? partesMedico[1] : "",
                    MedicoNombre = partesMedico[0],
                    Especialidad = t.especialidad!.nombre,
                    Estado = t.estado!
                };
            }).ToList();
        }

        public async Task<EstadisticasMesDTO> ObtenerEstadisticasMes(int mes, int anio, int? medicoId = null)
        {
            var primerDia = new DateOnly(anio, mes, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var queryTurnos = _context.turnos
                .Where(t => t.fecha >= primerDia && t.fecha <= ultimoDia);

            if (medicoId.HasValue)
                queryTurnos = queryTurnos.Where(t => t.medico_id == medicoId);

            var turnosMes = await queryTurnos.CountAsync();

            var turnosCancelados = await queryTurnos
                .Where(t => t.estado == "Cancelado")
                .CountAsync();

            var pacientesAtendidos = medicoId.HasValue 
                ? await queryTurnos
                    .Where(t => t.estado == "Finalizado")
                    .Select(t => t.paciente_id)
                    .Distinct()
                    .CountAsync()
                : 0;

            var pacientesTotales = await _context.pacientes.CountAsync();

            return new EstadisticasMesDTO
            {
                TurnosMes = turnosMes,
                TurnosCancelados = turnosCancelados,
                PacientesTotales = pacientesTotales,
                PacientesAtendidos = pacientesAtendidos
            };
        }

        public async Task<List<PacienteReporteDTO>> ObtenerTodosPacientes()
        {
            var pacientes = await _context.pacientes
                .Include(p => p.idNavigation)
                .Include(p => p.pacientesObrasSociales.Where(po => po.activa))
                    .ThenInclude(po => po.obrasocial)
                .OrderBy(p => p.idNavigation!.nombre)
                .ToListAsync();

            return pacientes.Select(p => {
                var nombreCompleto = p.idNavigation?.nombre ?? "";
                var partes = nombreCompleto.Split(' ', 2);
                
                return new PacienteReporteDTO
                {
                    Nombre = partes[0],
                    Apellido = partes.Length > 1 ? partes[1] : "",
                    DNI = p.dni ?? "",
                    Email = p.idNavigation?.email ?? "",
                    Telefono = p.telefono ?? "",
                    ObraSocial = p.pacientesObrasSociales.FirstOrDefault(po => po.activa)?.obrasocial?.nombre ?? "Sin obra social",
                    FechaRegistro = DateTime.Now.ToString("dd/MM/yyyy")
                };
            }).ToList();
        }

        public async Task<List<EntradaClinicaReporteDTO>> ObtenerEntradasClinicas(DateOnly fechaDesde, DateOnly fechaHasta, int medicoId)
        {
            var entradas = await _context.entradasclinicas
                .Include(e => e.historia)
                    .ThenInclude(h => h.paciente)
                        .ThenInclude(p => p.idNavigation)
                .Where(e => e.medico_id == medicoId && e.fecha >= fechaDesde && e.fecha <= fechaHasta)
                .OrderBy(e => e.fecha)
                .ToListAsync();

            return entradas.Select(e => {
                var nombrePaciente = e.historia.paciente.idNavigation?.nombre ?? "";
                var partes = nombrePaciente.Split(' ', 2);
                
                return new EntradaClinicaReporteDTO
                {
                    Fecha = e.fecha.ToString("dd/MM/yyyy"),
                    PacienteApellido = partes.Length > 1 ? partes[1] : "",
                    PacienteNombre = partes[0],
                    PacienteDNI = e.historia.paciente.dni ?? "",
                    Diagnostico = e.diagnostico,
                    Tratamiento = e.tratamiento,
                    Observaciones = e.observaciones ?? ""
                };
            }).ToList();
        }

        // ===== PDF GENERATION METHODS =====

        public byte[] GenerarPDFTurnos(List<TurnoReporteDTO> turnos, string titulo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph(titulo, titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                // Subtitle
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph subtitle = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", subtitleFont);
                subtitle.Alignment = Element.ALIGN_CENTER;
                subtitle.SpacingAfter = 20f;
                document.Add(subtitle);

                // Table
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 8f, 15f, 15f, 15f, 20f, 12f, 10f });

                // Headers
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255));
                string[] headers = { "Fecha", "Hora", "Pac. Apellido", "Pac. Nombre", "Médico", "Especialidad", "Estado", "DNI" };
                
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(59, 130, 246);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Padding = 8;
                    table.AddCell(cell);
                }

                // Data rows
                Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                Font whiteCellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(255, 255, 255));
                
                foreach (var turno in turnos)
                {
                    table.AddCell(new Phrase(turno.Fecha, cellFont));
                    table.AddCell(new Phrase(turno.Hora, cellFont));
                    table.AddCell(new Phrase(turno.PacienteApellido, cellFont));
                    table.AddCell(new Phrase(turno.PacienteNombre, cellFont));
                    table.AddCell(new Phrase(turno.MedicoApellido,cellFont));
                    table.AddCell(new Phrase(turno.Especialidad, cellFont));
                    
                    // Estado with color
                    var estadoColor = turno.Estado switch
                    {
                        "Reservado" => new BaseColor(34, 197, 94),      // Green
                        "Cancelado" => new BaseColor(239, 68, 68),      // Red
                        "Finalizado" => new BaseColor(59, 130, 246),    // Blue
                        "Reprogramado" => new BaseColor(251, 191, 36),  // Yellow
                        "Ausentado" => new BaseColor(156, 163, 175),    // Gray
                        "Disponible" => new BaseColor(255, 255, 255),   // White
                        _ => new BaseColor(255, 255, 255)
                    };
                    
                    var estadoFont = turno.Estado != "Disponible" ? whiteCellFont : cellFont;
                    PdfPCell estadoCell = new PdfPCell(new Phrase(turno.Estado, estadoFont));
                    estadoCell.BackgroundColor = estadoColor;
                    table.AddCell(estadoCell);
                    
                    table.AddCell(new Phrase(turno.PacienteDNI, cellFont));
                }

                document.Add(table);

                // Footer
                Paragraph footer = new Paragraph($"\n\nTotal de turnos: {turnos.Count}", subtitleFont);
                footer.Alignment = Element.ALIGN_RIGHT;
                document.Add(footer);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        public byte[] GenerarPDFPacientes(List<PacienteReporteDTO> pacientes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph("Listado de Pacientes", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                // Subtitle
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph subtitle = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", subtitleFont);
                subtitle.Alignment = Element.ALIGN_CENTER;
                subtitle.SpacingAfter = 20f;
                document.Add(subtitle);

                // Table
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 15f, 15f, 12f, 20f, 15f, 18f, 15f });

                // Headers
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255));
                string[] headers = { "Apellido", "Nombre", "DNI", "Email", "Teléfono", "Obra Social", "F. Registro" };
                
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(16, 185, 129);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Padding = 8;
                    table.AddCell(cell);
                }

                // Data rows
                Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                foreach (var paciente in pacientes)
                {
                    table.AddCell(new Phrase(paciente.Apellido, cellFont));
                    table.AddCell(new Phrase(paciente.Nombre, cellFont));
                    table.AddCell(new Phrase(paciente.DNI, cellFont));
                    table.AddCell(new Phrase(paciente.Email ?? "N/A", cellFont));
                    table.AddCell(new Phrase(paciente.Telefono ?? "N/A", cellFont));
                    table.AddCell(new Phrase(paciente.ObraSocial, cellFont));
                    table.AddCell(new Phrase(paciente.FechaRegistro, cellFont));
                }

                document.Add(table);

                // Footer
                Paragraph footer = new Paragraph($"\n\nTotal de pacientes: {pacientes.Count}", subtitleFont);
                footer.Alignment = Element.ALIGN_RIGHT;
                document.Add(footer);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        // ===== EXCEL GENERATION METHODS =====

        public byte[] GenerarExcelTurnos(List<TurnoReporteDTO> turnos, string nombreHoja)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(nombreHoja);

                // Headers
                worksheet.Cell(1, 1).Value = "Fecha";
                worksheet.Cell(1, 2).Value = "Hora";
                worksheet.Cell(1, 3).Value = "Paciente Apellido";
                worksheet.Cell(1, 4).Value = "Paciente Nombre";
                worksheet.Cell(1, 5).Value = "DNI Paciente";
                worksheet.Cell(1, 6).Value = "Médico Apellido";
                worksheet.Cell(1, 7).Value = "Médico Nombre";
                worksheet.Cell(1, 8).Value = "Especialidad";
                worksheet.Cell(1, 9).Value = "Estado";

                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                int row = 2;
                foreach (var turno in turnos)
                {
                    worksheet.Cell(row, 1).Value = turno.Fecha;
                    worksheet.Cell(row, 2).Value = turno.Hora;
                    worksheet.Cell(row, 3).Value = turno.PacienteApellido;
                    worksheet.Cell(row, 4).Value = turno.PacienteNombre;
                    worksheet.Cell(row, 5).Value = turno.PacienteDNI;
                    worksheet.Cell(row, 6).Value = turno.MedicoApellido;
                    worksheet.Cell(row, 7).Value = turno.MedicoNombre;
                    worksheet.Cell(row, 8).Value = turno.Especialidad;
                    worksheet.Cell(row, 9).Value = turno.Estado;

                    // Color estado
                    var estadoCell = worksheet.Cell(row, 9);
                    estadoCell.Style.Fill.BackgroundColor = turno.Estado switch
                    {
                        "Reservado" => XLColor.FromHtml("#22C55E"),      // Green
                        "Cancelado" => XLColor.FromHtml("#EF4444"),      // Red
                        "Finalizado" => XLColor.FromHtml("#3B82F6"),     // Blue
                        "Reprogramado" => XLColor.FromHtml("#FBBF24"),   // Yellow
                        "Ausentado" => XLColor.FromHtml("#9CA3AF"),      // Gray
                        "Disponible" => XLColor.White,                    // White
                        _ => XLColor.White
                    };
                    if (turno.Estado != "Disponible")
                        estadoCell.Style.Font.FontColor = XLColor.White;

                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] GenerarExcelPacientes(List<PacienteReporteDTO> pacientes)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Pacientes");

                // Headers
                worksheet.Cell(1, 1).Value = "Apellido";
                worksheet.Cell(1, 2).Value = "Nombre";
                worksheet.Cell(1, 3).Value = "DNI";
                worksheet.Cell(1, 4).Value = "Email";
                worksheet.Cell(1, 5).Value = "Teléfono";
                worksheet.Cell(1, 6).Value = "Obra Social";
                worksheet.Cell(1, 7).Value = "Fecha Registro";

                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                int row = 2;
                foreach (var paciente in pacientes)
                {
                    worksheet.Cell(row, 1).Value = paciente.Apellido;
                    worksheet.Cell(row, 2).Value = paciente.Nombre;
                    worksheet.Cell(row, 3).Value = paciente.DNI;
                    worksheet.Cell(row, 4).Value = paciente.Email ?? "N/A";
                    worksheet.Cell(row, 5).Value = paciente.Telefono ?? "N/A";
                    worksheet.Cell(row, 6).Value = paciente.ObraSocial;
                    worksheet.Cell(row, 7).Value = paciente.FechaRegistro;

                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] GenerarPDFHistoriasClinicas(List<EntradaClinicaReporteDTO> entradas, string titulo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph(titulo, titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                // Subtitle
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph subtitle = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", subtitleFont);
                subtitle.Alignment = Element.ALIGN_CENTER;
                subtitle.SpacingAfter = 20f;
                document.Add(subtitle);

                // Table
                PdfPTable table = new PdfPTable(7);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 15f, 15f, 12f, 20f, 20f, 20f });

                // Headers
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255));
                string[] headers = { "Fecha", "Pac. Apellido", "Pac. Nombre", "DNI", "Diagnóstico", "Tratamiento", "Observaciones" };
                
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(16, 185, 129); // green
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.Padding = 8;
                    table.AddCell(cell);
                }

                // Data rows
                Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                
                foreach (var entrada in entradas)
                {
                    table.AddCell(new Phrase(entrada.Fecha, cellFont));
                    table.AddCell(new Phrase(entrada.PacienteApellido, cellFont));
                    table.AddCell(new Phrase(entrada.PacienteNombre, cellFont));
                    table.AddCell(new Phrase(entrada.PacienteDNI, cellFont));
                    table.AddCell(new Phrase(entrada.Diagnostico, cellFont));
                    table.AddCell(new Phrase(entrada.Tratamiento, cellFont));
                    table.AddCell(new Phrase(entrada.Observaciones ?? "N/A", cellFont));
                }

                document.Add(table);

                // Footer
                Paragraph footer = new Paragraph($"\n\nTotal de entradas clínicas: {entradas.Count}", subtitleFont);
                footer.Alignment = Element.ALIGN_RIGHT;
                document.Add(footer);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        public byte[] GenerarExcelHistoriasClinicas(List<EntradaClinicaReporteDTO> entradas, string nombreHoja)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(nombreHoja);

                // Headers
                worksheet.Cell(1, 1).Value = "Fecha";
                worksheet.Cell(1, 2).Value = "Paciente Apellido";
                worksheet.Cell(1, 3).Value = "Paciente Nombre";
                worksheet.Cell(1, 4).Value = "DNI";
                worksheet.Cell(1, 5).Value = "Diagnóstico";
                worksheet.Cell(1, 6).Value = "Tratamiento";
                worksheet.Cell(1, 7).Value = "Observaciones";

                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                int row = 2;
                foreach (var entrada in entradas)
                {
                    worksheet.Cell(row, 1).Value = entrada.Fecha;
                    worksheet.Cell(row, 2).Value = entrada.PacienteApellido;
                    worksheet.Cell(row, 3).Value = entrada.PacienteNombre;
                    worksheet.Cell(row, 4).Value = entrada.PacienteDNI;
                    worksheet.Cell(row, 5).Value = entrada.Diagnostico;
                    worksheet.Cell(row, 6).Value = entrada.Tratamiento;
                    worksheet.Cell(row, 7).Value = entrada.Observaciones ?? "N/A";

                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
