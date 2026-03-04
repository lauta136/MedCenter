namespace MedCenter.Services.AdminService;

public class AdminResult
{
    public bool Success {get;set;}
    public string ErrorMessage{get;set;}
    public bool HasActiveTurnos {get;set;}
    public int ActiveTurnosCount {get;set;}
}