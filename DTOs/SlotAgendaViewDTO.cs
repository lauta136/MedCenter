namespace MedCenter.DTOs;

public class SlotAgendaViewDTO
{
    
    public int Id { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string HoraTexto => $"{HoraInicio:HH:mm} - {HoraFin:HH:mm}";
}