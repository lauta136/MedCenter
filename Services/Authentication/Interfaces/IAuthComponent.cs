using MedCenter.DTOs;
public interface IAuthComponent
{
    Task<AuthResult> AuthenticateAsync(string username, string password);
    Task<AuthResult> RegisterAsync(RegisterDTO registerDto);
}