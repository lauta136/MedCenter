namespace MedCenter.Model;

public class PasswordResetCode
{
    public int UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
    public int IntentosRestantes { get; set; } = 3; // Prevent brute force
}