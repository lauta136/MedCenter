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

    public class EstadisticasMesDTO
    {
        public int TurnosMes { get; set; }
        public int TurnosCancelados { get; set; }
        public int PacientesTotales { get; set; }
        public int PacientesAtendidos { get; set; }
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
