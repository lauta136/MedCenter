using MedCenter.Services.TurnoSv;

namespace MedCenter.DTOs
{
    public class TurnoReporteDTO
    {
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string PacienteNombre { get; set; }
        public string PacienteApellido { get; set; }
        public string PacienteDNI { get; set; }
        public string MedicoNombre { get; set; }
        public string MedicoApellido { get; set; }
        public string Especialidad { get; set; }
        public string Estado { get; set; }
    }
    public class TurnoAuditoriaReporteDTO
    {
        public required string PacienteNombre { get; set; }
        public required string PacienteApellido { get; set; }
        public required string PacienteDNI { get; set; }
        public required string MedicoNombre { get; set; }
        public required string MedicoApellido { get; set; }
        public required string Especialidad { get; set; }

        public DateOnly? FechaAnterior { get; set; }
        public DateOnly? FechaActual { get; set; }
    
        public TimeOnly? HoraAnterior { get; set; }
        public TimeOnly? HoraActual { get; set; }
    
        public EstadosTurno? EstadoAnterior { get; set; }
        public EstadosTurno? EstadoActual { get; set; }

        public string? MotivoCancelacion { get; set; }
    }

    public class EstadisticasMesDTO
    {
        public int TurnosMes { get; set; }
        public int TurnosCancelados { get; set; }
        public int PacientesTotales { get; set; }
        public int PacientesAtendidos { get; set; }
    }

    // NEW: Advanced statistics for decision-making (cumple requisito de KPIs)
    public class EstadisticasAvanzadasDTO
    {
        // Totales básicos
        public int TotalTurnos { get; set; }
        public int TurnosCompletados { get; set; }
        public int TurnosCancelados { get; set; }
        public int TurnosPendientes { get; set; }
        public int TurnosNoShow { get; set; }
        
        // KPIs calculados (información procesada)
        public decimal TasaCancelacion { get; set; }  // % de turnos cancelados
        public decimal TasaAsistencia { get; set; }    // % de turnos completados
        public decimal TasaNoShow { get; set; }        // % de inasistencias
        public decimal TasaOcupacion { get; set; }     // % de slots utilizados
        
        // Distribución por especialidad (cruce de datos)
        public Dictionary<string, int> TurnosPorEspecialidad { get; set; } = new();
        public Dictionary<string, decimal> TasaCancelacionPorEspecialidad { get; set; } = new();
        
        // Distribución por médico (top performers)
        public Dictionary<string, int> TurnosPorMedico { get; set; } = new();
        public Dictionary<string, decimal> TasaEfectividadPorMedico { get; set; } = new();
        
        // Tendencias temporales (asiste a toma de decisiones)
        public Dictionary<string, int> TurnosPorDia { get; set; } = new();
        public Dictionary<string, int> TurnosPorHorario { get; set; } = new();
        
        // Análisis de pacientes
        public int PacientesUnicos { get; set; }
        public decimal PromedioTurnosPorPaciente { get; set; }
        public Dictionary<string, int> PacientesPorObraSocial { get; set; } = new();
        
        // Tiempo promedio entre reserva y atención
        public double DiasPromedioReservaAtencion { get; set; }
        
        // Especialidad más demandada
        public string? EspecialidadMasDemandada { get; set; }
        public string? MedicoMasEfectivo { get; set; }
        public string? DiaConMasTurnos { get; set; }
        public string? HorarioMasDemandado { get; set; }
    }

    public class PacienteReporteDTO
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string DNI { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string ObraSocial { get; set; }
        public string FechaRegistro { get; set; }
    }

    public class EntradaClinicaReporteDTO
    {
        public string? Fecha { get; set; }
        public string? PacienteApellido { get; set; }
        public string? PacienteNombre { get; set; }
        public string? PacienteDNI { get; set; }
        public string? Diagnostico { get; set; }
        public string? Tratamiento { get; set; }
        public string? Observaciones { get; set; }
    }
}
