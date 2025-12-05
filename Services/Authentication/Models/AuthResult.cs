using MedCenter.Services.TurnoSv;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message{get;set;}
    public RolUsuario? Role { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserMail{ get; set; }
}