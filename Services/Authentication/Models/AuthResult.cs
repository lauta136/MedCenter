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
    /// <summary>
    /// True when the account was found but access was denied (deactivated, wrong password).
    /// The composite should stop trying other authenticators and surface this error directly.
    /// </summary>
    public bool IsDefinitive { get; set; }
}