using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using MedCenter.Data;
using MedCenter.DTOs;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.TurnoSv;
using MedCenter.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

        // NEW: Calculate advanced statistics for decision-making (cumple requisito)
        public async Task<EstadisticasAvanzadasDTO> CalcularEstadisticasAvanzadas(
            DateTime fechaDesde, 
            DateTime fechaHasta,
            int? medicoId = null,
            int? especialidadId = null)
        {
            var desdeOnly = DateOnly.FromDateTime(fechaDesde);
            var hastaOnly = DateOnly.FromDateTime(fechaHasta);

            var query = _context.turnos
                .Include(t => t.paciente).ThenInclude(p => p!.idNavigation)
                .Include(t => t.medico).ThenInclude(m => m!.idNavigation)
                .Include(t => t.especialidad)
                .Include(t => t.paciente!.pacientesObrasSociales.Where(po => po.activa))
                    .ThenInclude(po => po.obrasocial)
                .Where(t => t.fecha >= desdeOnly && t.fecha <= hastaOnly);

            if (medicoId.HasValue)
                query = query.Where(t => t.medico_id == medicoId);
            if (especialidadId.HasValue)
                query = query.Where(t => t.especialidad_id == especialidadId);

            var turnos = await query.ToListAsync();
            var totalTurnos = turnos.Count;

            if (totalTurnos == 0)
                return new EstadisticasAvanzadasDTO();

            // Totales por estado (estado es string en el modelo)
            var completados = turnos.Count(t => t.estado == "Finalizado");
            var cancelados = turnos.Count(t => t.estado == "Cancelado");
            var pendientes = turnos.Count(t => t.estado == "Pendiente");
            var noShow = turnos.Count(t => t.estado == "Ausentado");

            // KPIs calculados (procesamiento de información)
            var tasaCancelacion = totalTurnos > 0 ? (decimal)cancelados / totalTurnos * 100 : 0;
            var tasaAsistencia = totalTurnos > 0 ? (decimal)completados / totalTurnos * 100 : 0;
            var tasaNoShow = totalTurnos > 0 ? (decimal)noShow / totalTurnos * 100 : 0;

            // Cruce de datos: Turnos por especialidad
            var turnosPorEsp = turnos
                .GroupBy(t => t.especialidad?.nombre ?? "Sin especialidad")
                .ToDictionary(g => g.Key, g => g.Count());

            // KPI por especialidad: Tasa de cancelación
            var tasaCancelacionPorEsp = turnos
                .GroupBy(t => t.especialidad?.nombre ?? "Sin especialidad")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 ? (decimal)g.Count(t => t.estado == "Cancelado") / g.Count() * 100 : 0
                );

            // Top performers: Médicos más efectivos (completados / total)
            var turnosPorMedico = turnos
                .GroupBy(t => $"{t.medico?.idNavigation?.nombre}")
                .ToDictionary(g => g.Key, g => g.Count());

            var efectividadPorMedico = turnos
                .GroupBy(t => $"{t.medico?.idNavigation?.nombre}")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 ? (decimal)g.Count(t => t.estado == "Finalizado") / g.Count() * 100 : 0
                );

            // Tendencias: Distribución por día de semana
            var turnosPorDia = turnos
                .Where(t => t.fecha.HasValue)
                .GroupBy(t => t.fecha!.Value.DayOfWeek.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Distribución por franja horaria (Mañana/Tarde)
            var turnosPorHorario = turnos
                .Where(t => t.hora.HasValue)
                .GroupBy(t => t.hora!.Value.Hour < 14 ? "Mañana (8-14hs)" : "Tarde (14-20hs)")
                .ToDictionary(g => g.Key, g => g.Count());

            // Análisis de pacientes
            var pacientesUnicos = turnos.Select(t => t.paciente_id).Distinct().Count();
            var promedioTurnosPorPaciente = pacientesUnicos > 0 ? (decimal)totalTurnos / pacientesUnicos : 0;

            var pacientesPorObraSocial = turnos
                .Where(t => t.paciente != null)
                .SelectMany(t => t.paciente!.pacientesObrasSociales
                    .Where(po => po.activa)
                    .Select(po => po.obrasocial?.nombre ?? "Sin obra social"))
                .GroupBy(os => os)
                .ToDictionary(g => g.Key, g => g.Count());

            // Insights para toma de decisiones
            var especialidadMasDemandada = turnosPorEsp.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            var medicoMasEfectivo = efectividadPorMedico.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            var diaConMasTurnos = turnosPorDia.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            var horarioMasDemandado = turnosPorHorario.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            return new EstadisticasAvanzadasDTO
            {
                // Totales
                TotalTurnos = totalTurnos,
                TurnosCompletados = completados,
                TurnosCancelados = cancelados,
                TurnosPendientes = pendientes,
                TurnosNoShow = noShow,
                
                // KPIs
                TasaCancelacion = Math.Round(tasaCancelacion, 2),
                TasaAsistencia = Math.Round(tasaAsistencia, 2),
                TasaNoShow = Math.Round(tasaNoShow, 2),
                TasaOcupacion = Math.Round(tasaAsistencia + (decimal)pendientes / totalTurnos * 100, 2),
                
                // Distribuciones
                TurnosPorEspecialidad = turnosPorEsp,
                TasaCancelacionPorEspecialidad = tasaCancelacionPorEsp.ToDictionary(k => k.Key, v => Math.Round(v.Value, 2)),
                TurnosPorMedico = turnosPorMedico,
                TasaEfectividadPorMedico = efectividadPorMedico.ToDictionary(k => k.Key, v => Math.Round(v.Value, 2)),
                TurnosPorDia = turnosPorDia,
                TurnosPorHorario = turnosPorHorario,
                
                // Pacientes
                PacientesUnicos = pacientesUnicos,
                PromedioTurnosPorPaciente = Math.Round(promedioTurnosPorPaciente, 2),
                PacientesPorObraSocial = pacientesPorObraSocial,
                
                // Insights
                EspecialidadMasDemandada = especialidadMasDemandada,
                MedicoMasEfectivo = medicoMasEfectivo,
                DiaConMasTurnos = diaConMasTurnos,
                HorarioMasDemandado = horarioMasDemandado
            };
        }

        // NEW: Generate Executive PDF with advanced statistics and charts
        public byte[] GenerarPDFEstadisticasAvanzadas(EstadisticasAvanzadasDTO stats, string titulo, DateTime fechaDesde, DateTime fechaHasta)
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            PdfWriter.GetInstance(document, ms);
            document.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
            var title = new Paragraph(titulo, titleFont) { Alignment = Element.ALIGN_CENTER };
            document.Add(title);

            var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var subtitle = new Paragraph($"Período: {fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}", subtitleFont) 
            { 
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20 
            };
            document.Add(subtitle);

            // Section 1: KPIs Principales
            var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var kpisTitle = new Paragraph("📊 Indicadores Clave de Rendimiento (KPIs)", sectionFont) { SpacingAfter = 10 };
            document.Add(kpisTitle);

            var kpisTable = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 15 };
            kpisTable.SetWidths(new float[] { 1f, 1f, 1f, 1f });
            
            AddKPICell(kpisTable, "Total Turnos", stats.TotalTurnos.ToString(), new BaseColor(211, 211, 211));
            AddKPICell(kpisTable, "Tasa Asistencia", $"{stats.TasaAsistencia}%", new BaseColor(144, 238, 144));
            AddKPICell(kpisTable, "Tasa Cancelación", $"{stats.TasaCancelacion}%", new BaseColor(255, 182, 193));
            AddKPICell(kpisTable, "Tasa No Show", $"{stats.TasaNoShow}%", new BaseColor(255, 200, 124));
            
            document.Add(kpisTable);

            // Section 2: Simple Bar Chart - Turnos por Estado
            document.Add(new Paragraph("📈 Distribución de Turnos por Estado", sectionFont) { SpacingAfter = 10 });
            AddSimpleBarChart(document, new Dictionary<string, int>
            {
                { "Completados", stats.TurnosCompletados },
                { "Cancelados", stats.TurnosCancelados },
                { "Pendientes", stats.TurnosPendientes },
                { "Ausentados", stats.TurnosNoShow }
            });

            // Section 3: Top Insights
            document.Add(new Paragraph("💡 Insights Estratégicos", sectionFont) { SpacingAfter = 10 });
            
            var insightsTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 15 };
            insightsTable.SetWidths(new float[] { 1.5f, 1f });
            
            AddInsightRow(insightsTable, "Especialidad más demandada:", stats.EspecialidadMasDemandada ?? "N/A");
            AddInsightRow(insightsTable, "Médico más efectivo:", stats.MedicoMasEfectivo ?? "N/A");
            AddInsightRow(insightsTable, "Día con más turnos:", stats.DiaConMasTurnos ?? "N/A");
            AddInsightRow(insightsTable, "Horario más demandado:", stats.HorarioMasDemandado ?? "N/A");
            AddInsightRow(insightsTable, "Pacientes únicos atendidos:", stats.PacientesUnicos.ToString());
            AddInsightRow(insightsTable, "Promedio turnos/paciente:", stats.PromedioTurnosPorPaciente.ToString("F2"));
            
            document.Add(insightsTable);

            // Section 4: Turnos por Especialidad
            if (stats.TurnosPorEspecialidad.Any())
            {
                document.Add(new Paragraph("🏥 Distribución por Especialidad", sectionFont) { SpacingAfter = 10 });
                
                var espTable = new PdfPTable(3) { WidthPercentage = 100, SpacingAfter = 15 };
                espTable.SetWidths(new float[] { 2f, 1f, 1f });
                
                AddHeaderCell(espTable, "Especialidad");
                AddHeaderCell(espTable, "Turnos");
                AddHeaderCell(espTable, "% Cancelación");
                
                foreach (var esp in stats.TurnosPorEspecialidad.OrderByDescending(x => x.Value).Take(5))
                {
                    AddDataCell(espTable, esp.Key);
                    AddDataCell(espTable, esp.Value.ToString());
                    var cancelRate = stats.TasaCancelacionPorEspecialidad.GetValueOrDefault(esp.Key, 0);
                    AddDataCell(espTable, $"{cancelRate}%");
                }
                
                document.Add(espTable);
            }

            // Section 5: Recomendaciones
            document.Add(new Paragraph("📋 Recomendaciones Basadas en Datos", sectionFont) { SpacingAfter = 10 });
            
            var recommendations = new List<string>();
            if (stats.TasaCancelacion > 15)
                recommendations.Add($"• Alta tasa de cancelación ({stats.TasaCancelacion}%). Considere implementar recordatorios automáticos.");
            if (stats.TasaNoShow > 10)
                recommendations.Add($"• Tasa de inasistencia elevada ({stats.TasaNoShow}%). Revise políticas de confirmación.");
            if (stats.TurnosPorHorario.ContainsKey("Tarde (14-20hs)") && 
                stats.TurnosPorHorario["Tarde (14-20hs)"] > stats.TurnosPorHorario.GetValueOrDefault("Mañana (8-14hs)", 0) * 1.5m)
                recommendations.Add("• Mayor demanda en horario vespertino. Considere ampliar turnos tarde.");
            if (recommendations.Count == 0)
                recommendations.Add("• Los indicadores están dentro de rangos normales. Mantener prácticas actuales.");
            
            var recFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            foreach (var rec in recommendations)
            {
                document.Add(new Paragraph(rec, recFont) { SpacingAfter = 5 });
            }

            // Footer
            document.Add(new Paragraph($"\nReporte generado: {DateTime.Now:dd/MM/yyyy HH:mm}", 
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8)) 
            { 
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = 20
            });

            document.Close();
            return ms.ToArray();
        }

        // NEW: Generate simple statistics PDF for summary reports (without detailed data)
        public byte[] GenerarPDFEstadisticasSimples(EstadisticasMesDTO stats, string titulo, DateTime fechaDesde, DateTime fechaHasta)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 50, 50, 50, 50);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
                Paragraph title = new Paragraph(titulo, titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 10f;
                document.Add(title);

                // Period subtitle
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Paragraph subtitle = new Paragraph($"Período: {fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}", subtitleFont);
                subtitle.Alignment = Element.ALIGN_CENTER;
                subtitle.SpacingAfter = 30f;
                document.Add(subtitle);

                // Statistics table
                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 80;
                table.SpacingAfter = 20f;
                table.HorizontalAlignment = Element.ALIGN_CENTER;
                table.SetWidths(new float[] { 60f, 40f });

                Font labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                Font valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 14);

                // Add statistics rows
                AddStatRow(table, "Total de Turnos en el Período:", stats.TurnosMes.ToString(), labelFont, valueFont, new BaseColor(59, 130, 246));
                AddStatRow(table, "Turnos Cancelados:", stats.TurnosCancelados.ToString(), labelFont, valueFont, new BaseColor(239, 68, 68));
                
                if (stats.PacientesAtendidos > 0)
                {
                    AddStatRow(table, "Pacientes Atendidos:", stats.PacientesAtendidos.ToString(), labelFont, valueFont, new BaseColor(34, 197, 94));
                }
                
                AddStatRow(table, "Total de Pacientes Registrados:", stats.PacientesTotales.ToString(), labelFont, valueFont, new BaseColor(156, 163, 175));

                // Calculate percentage
                if (stats.TurnosMes > 0)
                {
                    var tasaCancelacion = (decimal)stats.TurnosCancelados / stats.TurnosMes * 100;
                    AddStatRow(table, "Tasa de Cancelación:", $"{tasaCancelacion:F2}%", labelFont, valueFont, 
                        tasaCancelacion > 20 ? new BaseColor(239, 68, 68) : new BaseColor(251, 191, 36));
                }

                document.Add(table);

                // Footer
                Paragraph footer = new Paragraph(
                    $"\nReporte generado: {DateTime.Now:dd/MM/yyyy HH:mm}", 
                    FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10)
                );
                footer.Alignment = Element.ALIGN_RIGHT;
                footer.SpacingBefore = 30f;
                document.Add(footer);

                document.Close();
                return ms.ToArray();
            }
        }

        private void AddStatRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont, BaseColor valueColor)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.Padding = 10;
            labelCell.HorizontalAlignment = Element.ALIGN_LEFT;
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
            valueCell.BackgroundColor = valueColor;
            valueCell.Padding = 10;
            valueCell.HorizontalAlignment = Element.ALIGN_CENTER;
            Font whiteCellFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(255, 255, 255));
            valueCell.Phrase = new Phrase(value, whiteCellFont);
            table.AddCell(valueCell);
        }

        // Helper methods for PDF generation
        private void AddKPICell(PdfPTable table, string label, string value, BaseColor color)
        {
            var cell = new PdfPCell();
            cell.BackgroundColor = color;
            cell.Padding = 8;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            
            var labelFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            var valueFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            
            var phrase = new Phrase();
            phrase.Add(new Chunk(label + "\n", labelFont));
            phrase.Add(new Chunk(value, valueFont));
            cell.Phrase = phrase;
            
            table.AddCell(cell);
        }

        private void AddSimpleBarChart(Document document, Dictionary<string, int> data)
        {
            var maxValue = data.Values.Max();
            var chartTable = new PdfPTable(2) { WidthPercentage = 100, SpacingAfter = 15 };
            chartTable.SetWidths(new float[] { 1.5f, 3f });
            
            foreach (var item in data.OrderByDescending(x => x.Value))
            {
                // Label
                var labelCell = new PdfPCell(new Phrase(item.Key, FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                labelCell.Border = Rectangle.NO_BORDER;
                labelCell.PaddingRight = 5;
                labelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                chartTable.AddCell(labelCell);
                
                // Bar
                var barCell = new PdfPCell();
                barCell.Border = Rectangle.NO_BORDER;
                
                var barWidth = maxValue > 0 ? (float)item.Value / maxValue : 0;
                var barTable = new PdfPTable(2) { WidthPercentage = 100 };
                barTable.SetWidths(new float[] { barWidth, 1 - barWidth });
                
                var filledCell = new PdfPCell(new Phrase($" {item.Value}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)));
                filledCell.BackgroundColor = new BaseColor(135, 206, 250);  // Sky blue
                filledCell.Border = Rectangle.NO_BORDER;
                filledCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                filledCell.PaddingRight = 5;
                
                var emptyCell = new PdfPCell();
                emptyCell.Border = Rectangle.NO_BORDER;
                
                barTable.AddCell(filledCell);
                barTable.AddCell(emptyCell);
                
                barCell.AddElement(barTable);
                chartTable.AddCell(barCell);
            }
            
            document.Add(chartTable);
        }

        private void AddInsightRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            labelCell.Border = Rectangle.NO_BORDER;
            labelCell.PaddingBottom = 8;
            table.AddCell(labelCell);
            
            var valueCell = new PdfPCell(new Phrase(value, FontFactory.GetFont(FontFactory.HELVETICA, 10)));
            valueCell.Border = Rectangle.NO_BORDER;
            valueCell.PaddingBottom = 8;
            table.AddCell(valueCell);
        }

        private void AddHeaderCell(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            cell.BackgroundColor = new BaseColor(211, 211, 211);  // Light gray
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
        }

        private void AddDataCell(PdfPTable table, string text)
        {
            var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 9)));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            table.AddCell(cell);
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

        public async Task<List<TurnoAuditoriaReporteDTO>> ObtenerAuditoriaTurnos(DateTime fechaDesde, DateTime fechaHasta, string? usuarioNombre, AccionesTurno? accion, int? pacienteId, int? medicoId)
        {
            // Convert to UTC to match PostgreSQL timestamp with time zone
            var desdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            var hastaUtc = DateTime.SpecifyKind(fechaHasta.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            
            var query = _context.turnoAuditorias.Where(e => e.MomentoAccion >= desdeUtc && e.MomentoAccion <= hastaUtc);

            if (usuarioNombre != null)
                query = query.Where(e => e.UsuarioNombre == usuarioNombre);
            if (accion != null)
                query = query.Where(e => e.Accion == accion);
            if (pacienteId != null)
                query = query.Where(e => e.PacienteId == pacienteId);
            if (medicoId != null)
                query = query.Where(e => e.MedicoId == medicoId);

            var audits = await query.OrderBy(e => e.MomentoAccion).ToListAsync();

            return audits.Select(e =>
            {
                var nombrePaciente = e.PacienteNombre ?? "";
                var partesPaciente = nombrePaciente.Split(' ', 2);

                var nombreMedico = e.MedicoNombre ?? "";
                var partesMedico = nombreMedico.Split(' ', 2);

                return new TurnoAuditoriaReporteDTO
                {
                    FechaAnterior = e.FechaAnterior,
                    FechaActual = e.FechaNueva,
                    HoraAnterior = e.HoraAnterior,
                    HoraActual = e.HoraNueva,
                    PacienteApellido = partesPaciente.Length > 1 ? partesPaciente[1] : "",
                    PacienteNombre = partesPaciente[0],
                    PacienteDNI = e.PacienteDNI,
                    MedicoNombre = partesMedico[0],
                    MedicoApellido = partesMedico.Length > 1 ? partesMedico[1] : "",
                    Especialidad = e.EspecialidadNombre,
                    EstadoAnterior = e.EstadoAnterior,
                    EstadoActual = e.EstadoNuevo,
                    MotivoCancelacion = string.IsNullOrEmpty(e.MotivoCancelacion) ? "" : e.MotivoCancelacion
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

         public byte[] GenerarPDFAuditoriaTurnos(List<TurnoAuditoriaReporteDTO> turnosAudit)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 15, 15, 20, 20);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                
                document.Open();

                // Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Paragraph title = new Paragraph("Registro de auditoría de Turnos", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 15f;
                document.Add(title);

                // Subtitle
                Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                Paragraph subtitle = new Paragraph($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", subtitleFont);
                subtitle.Alignment = Element.ALIGN_CENTER;
                subtitle.SpacingAfter = 15f;
                document.Add(subtitle);

                // Table with 10 columns (combined some columns)
                PdfPTable table = new PdfPTable(10);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 12f, 8f, 12f, 14f, 10f, 10f, 10f, 10f, 8f, 8f });

                // Headers
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, new BaseColor(255, 255, 255));
                string[] headers = { "Paciente", "DNI", "Médico", "Especialidad", "Fecha Ant.", "Hora Ant.", "Fecha Act.", "Hora Act.", "Est. Ant.", "Est. Act." };
                
                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(16, 185, 129);
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 5;
                    table.AddCell(cell);
                }

                // Data rows
                Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 6.5f);
                foreach (var turno in turnosAudit)
                {
                    // Paciente (apellido + nombre)
                    string paciente = $"{turno.PacienteApellido} {turno.PacienteNombre}".Trim();
                    table.AddCell(new Phrase(paciente, cellFont));
                    
                    // DNI
                    table.AddCell(new Phrase(turno.PacienteDNI, cellFont));
                    
                    // Médico (apellido + nombre)
                    string medico = turno.MedicoApellido != null 
                        ? $"{turno.MedicoApellido} {turno.MedicoNombre}".Trim() 
                        : "N/A";
                    table.AddCell(new Phrase(medico, cellFont));
                    
                    // Especialidad
                    table.AddCell(new Phrase(turno.Especialidad ?? "N/A", cellFont));
                    
                    // Fecha/Hora Anterior
                    table.AddCell(new Phrase(turno.FechaAnterior?.ToString("dd/MM/yy") ?? "-", cellFont));
                    table.AddCell(new Phrase(turno.HoraAnterior?.ToString(@"hh\:mm") ?? "-", cellFont));
                    
                    // Fecha/Hora Actual
                    table.AddCell(new Phrase(turno.FechaActual?.ToString("dd/MM/yy") ?? "-", cellFont));
                    table.AddCell(new Phrase(turno.HoraActual?.ToString(@"hh\:mm") ?? "-", cellFont));
                    
                    // Estados (abbreviated)
                    string estadoAnt = turno.EstadoAnterior?.ToString() ?? "-";
                    string estadoAct = turno.EstadoActual?.ToString() ?? "-";
                    
                    // Abbreviate estado names to save space
                    estadoAnt = estadoAnt.Replace("Reservado", "Res.")
                                         .Replace("Cancelado", "Can.")
                                         .Replace("Finalizado", "Fin.")
                                         .Replace("Reprogramado", "Rep.")
                                         .Replace("Ausentado", "Aus.");
                    estadoAct = estadoAct.Replace("Reservado", "Res.")
                                         .Replace("Cancelado", "Can.")
                                         .Replace("Finalizado", "Fin.")
                                         .Replace("Reprogramado", "Rep.")
                                         .Replace("Ausentado", "Aus.");
                    
                    table.AddCell(new Phrase(estadoAnt, cellFont));
                    table.AddCell(new Phrase(estadoAct, cellFont));
                }

                document.Add(table);

                // Footer with note
                Paragraph footer = new Paragraph($"\n\nTotal de entradas: {turnosAudit.Count}", subtitleFont);
                footer.Alignment = Element.ALIGN_RIGHT;
                document.Add(footer);
                
                // Legend
                Font legendFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
                Paragraph legend = new Paragraph("\nEstados: Res.=Reservado, Can.=Cancelado, Fin.=Finalizado, Rep.=Reprogramado, Aus.=Ausentado", legendFont);
                legend.Alignment = Element.ALIGN_LEFT;
                document.Add(legend);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }        // ===== EXCEL GENERATION METHODS =====

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

        public byte[] GenerarExcelAuditoriaTurnos(List<TurnoAuditoriaReporteDTO> turnosAudit)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Auditoría Turnos");

                // Headers
                worksheet.Cell(1, 1).Value = "Apellido Paciente";
                worksheet.Cell(1, 2).Value = "Nombre Paciente";
                worksheet.Cell(1, 3).Value = "DNI Paciente";
                worksheet.Cell(1, 4).Value = "Apellido Médico";
                worksheet.Cell(1, 5).Value = "Nombre Médico";
                worksheet.Cell(1, 6).Value = "Especialidad";
                worksheet.Cell(1, 7).Value = "Fecha Anterior";
                worksheet.Cell(1, 8).Value = "Hora Anterior";
                worksheet.Cell(1, 9).Value = "Fecha Actual";
                worksheet.Cell(1, 10).Value = "Hora Actual";
                worksheet.Cell(1, 11).Value = "Estado Anterior";
                worksheet.Cell(1, 12).Value = "Estado Actual";
                worksheet.Cell(1, 13).Value = "Motivo Cancelación";

                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 13);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#10B981");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Data
                int row = 2;
                foreach (var turno in turnosAudit)
                {
                    worksheet.Cell(row, 1).Value = turno.PacienteApellido;
                    worksheet.Cell(row, 2).Value = turno.PacienteNombre;
                    worksheet.Cell(row, 3).Value = turno.PacienteDNI;
                    worksheet.Cell(row, 4).Value = turno.MedicoApellido ?? "N/A";
                    worksheet.Cell(row, 5).Value = turno.MedicoNombre ?? "N/A";
                    worksheet.Cell(row, 6).Value = turno.Especialidad;
                    worksheet.Cell(row, 7).Value = turno.FechaAnterior?.ToString("dd/MM/yyyy") ?? "N/A";
                    worksheet.Cell(row, 8).Value = turno.HoraAnterior?.ToString(@"hh\:mm") ?? "N/A";
                    worksheet.Cell(row, 9).Value = turno.FechaActual?.ToString("dd/MM/yyyy") ?? "N/A";
                    worksheet.Cell(row, 10).Value = turno.HoraActual?.ToString(@"hh\:mm") ?? "N/A";
                    worksheet.Cell(row, 11).Value = turno.EstadoAnterior?.ToString() ?? "N/A";
                    worksheet.Cell(row, 12).Value = turno.EstadoActual?.ToString() ?? "N/A";
                    worksheet.Cell(row, 13).Value = turno.MotivoCancelacion ?? "N/A";

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

        // ===== LOGIN AUDIT METHODS =====

        public async Task<List<LoginAuditoriaReporteDTO>> ObtenerAuditoriaLogins(
            DateTime fechaDesde, 
            DateTime fechaHasta, 
            string? usuarioNombre, 
            TipoLogout? tipoLogout, 
            RolUsuario? rol)
        {
            // Ensure UTC for PostgreSQL and include the entire end day
            var fechaDesdeUtc = DateTime.SpecifyKind(fechaDesde, DateTimeKind.Utc);
            // Add 23:59:59 to include the entire last day
            var fechaHastaUtc = DateTime.SpecifyKind(fechaHasta.AddHours(23).AddMinutes(59).AddSeconds(59), DateTimeKind.Utc);
            
            var query = _context.trazabilidadLogins.AsQueryable();

            // Filter by date range
            query = query.Where(tl => tl.MomentoLogin >= fechaDesdeUtc && tl.MomentoLogin <= fechaHastaUtc);

            // Filter by user name
            if (!string.IsNullOrEmpty(usuarioNombre))
            {
                query = query.Where(tl => tl.UsuarioNombre.Contains(usuarioNombre));
            }

            // Filter by logout type
            if (tipoLogout.HasValue)
            {
                query = query.Where(tl => tl.TipoLogout == tipoLogout.Value);
            }

            // Filter by role
            if (rol.HasValue)
            {
                query = query.Where(tl => tl.UsuarioRol == rol.Value);
            }

            var loginsUtc = await query
                .OrderByDescending(tl => tl.MomentoLogin)
                .Select(tl => new LoginAuditoriaReporteDTO
                {
                    Id = tl.Id,
                    UsuarioNombre = tl.UsuarioNombre,
                    UsuarioRol = tl.UsuarioRol.ToString(),
                    MomentoLogin = tl.MomentoLogin,
                    MomentoLogout = tl.MomentoLogout,
                    TipoLogout = tl.TipoLogout.HasValue ? tl.TipoLogout.Value.ToString() : null,
                    DuracionSesion = tl.MomentoLogout.HasValue 
                        ? tl.MomentoLogout.Value - tl.MomentoLogin 
                        : null
                })
                .ToListAsync();

            // Convert UTC times to local time for display
            var logins = loginsUtc.Select(l => new LoginAuditoriaReporteDTO
            {
                Id = l.Id,
                UsuarioNombre = l.UsuarioNombre,
                UsuarioRol = l.UsuarioRol,
                MomentoLogin = DateTime.SpecifyKind(l.MomentoLogin, DateTimeKind.Utc).ToLocalTime(),
                MomentoLogout = l.MomentoLogout.HasValue 
                    ? DateTime.SpecifyKind(l.MomentoLogout.Value, DateTimeKind.Utc).ToLocalTime() 
                    : (DateTime?)null,
                TipoLogout = l.TipoLogout,
                DuracionSesion = l.DuracionSesion
            }).ToList();

            return logins;
        }

        public byte[] GenerarPDFAuditoriaLogins(List<LoginAuditoriaReporteDTO> logins)
        {
            using (var ms = new MemoryStream())
            {
                var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.Black);
                var title = new Paragraph("Reporte de Auditoría de Login/Logout\n\n", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);

                // Summary
                var summaryFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.Black);
                var summary = new Paragraph($"Total de sesiones: {logins.Count}\n" +
                    $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n", summaryFont);
                document.Add(summary);

                // Table
                var table = new PdfPTable(7) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 15f, 15f, 20f, 20f, 15f, 10f, 15f });

                // Header
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.White);
                var headerBackground = new BaseColor(52, 58, 64);
                
                string[] headers = { "Usuario", "Rol", "Login", "Logout", "Tipo Logout", "Duración", "Estado" };
                foreach (var header in headers)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = headerBackground,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 8
                    };
                    table.AddCell(cell);
                }

                // Data rows
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.Black);
                foreach (var login in logins)
                {
                    table.AddCell(new PdfPCell(new Phrase(login.UsuarioNombre, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(login.UsuarioRol, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(login.MomentoLogin.ToString("dd/MM/yyyy HH:mm:ss"), cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(login.MomentoLogout?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Activa", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(login.TipoLogout ?? "-", cellFont)) { Padding = 5 });
                    
                    string duracion = "-";
                    if (login.DuracionSesion.HasValue)
                    {
                        var ts = login.DuracionSesion.Value;
                        duracion = ts.TotalHours >= 1 
                            ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                            : $"{ts.Minutes}m {ts.Seconds}s";
                    }
                    table.AddCell(new PdfPCell(new Phrase(duracion, cellFont)) { Padding = 5 });
                    
                    var estado = login.MomentoLogout.HasValue ? "Cerrada" : "Activa";
                    var estadoColor = login.MomentoLogout.HasValue ? BaseColor.Black : new BaseColor(0, 128, 0);
                    var estadoFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, estadoColor);
                    table.AddCell(new PdfPCell(new Phrase(estado, estadoFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                }

                document.Add(table);

                // Footer with statistics
                document.Add(new Paragraph("\n"));
                var statsFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.Black);
                
                var sesionesActivas = logins.Count(l => !l.MomentoLogout.HasValue);
                var sesionesCerradas = logins.Count(l => l.MomentoLogout.HasValue);
                var porRol = logins.GroupBy(l => l.UsuarioRol).ToDictionary(g => g.Key, g => g.Count());
                var porTipoLogout = logins.Where(l => l.TipoLogout != null).GroupBy(l => l.TipoLogout).ToDictionary(g => g.Key!, g => g.Count());

                var stats = new Paragraph("Estadísticas:\n", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.Black));
                stats.Add(new Phrase($"• Sesiones activas: {sesionesActivas}\n", statsFont));
                stats.Add(new Phrase($"• Sesiones cerradas: {sesionesCerradas}\n", statsFont));
                stats.Add(new Phrase($"• Por rol: {string.Join(", ", porRol.Select(p => $"{p.Key} ({p.Value})"))}\n", statsFont));
                if (porTipoLogout.Any())
                {
                    stats.Add(new Phrase($"• Por tipo logout: {string.Join(", ", porTipoLogout.Select(p => $"{p.Key} ({p.Value})"))}\n", statsFont));
                }

                document.Add(stats);

                document.Close();
                return ms.ToArray();
            }
        }

        public byte[] GenerarExcelAuditoriaLogins(List<LoginAuditoriaReporteDTO> logins)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Auditoría Logins");

                // Title
                worksheet.Cell(1, 1).Value = "Reporte de Auditoría de Login/Logout";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Range(1, 1, 1, 7).Merge();

                // Headers
                var headers = new[] { "Usuario", "Rol", "Login", "Logout", "Tipo Logout", "Duración (min)", "Estado" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(3, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Data
                int row = 4;
                foreach (var login in logins)
                {
                    worksheet.Cell(row, 1).Value = login.UsuarioNombre;
                    worksheet.Cell(row, 2).Value = login.UsuarioRol;
                    worksheet.Cell(row, 3).Value = login.MomentoLogin.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cell(row, 4).Value = login.MomentoLogout?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Activa";
                    worksheet.Cell(row, 5).Value = login.TipoLogout ?? "-";
                    worksheet.Cell(row, 6).Value = login.DuracionSesion?.TotalMinutes.ToString("F2") ?? "-";
                    worksheet.Cell(row, 7).Value = login.MomentoLogout.HasValue ? "Cerrada" : "Activa";
                    
                    if (!login.MomentoLogout.HasValue)
                    {
                        worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
                        worksheet.Cell(row, 7).Style.Font.Bold = true;
                    }
                    
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
