namespace MedCenter.Services.TurnoSv;

public class TurnoSvReprogamarResult()
{
    public bool success { get; set; }
    public string message { get; set; }
    public DateOnly fecha {get;set;}
    public TimeOnly hora{get;set;}

    public string pacienteNombre {get;set;}
    public string medicoNombre{get;set;}
}