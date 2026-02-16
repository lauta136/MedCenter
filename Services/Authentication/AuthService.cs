using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Services.Authentication.Components;
using MedCenter.Services.Authentication;
using DocumentFormat.OpenXml.Bibliography;
using MedCenter.Models;

public class AuthService
{
    private readonly AuthComposite _authComposite;

    
    public AuthService( 
    PacienteAuthenticator pacienteAuthenticator,
    MedicoAuthenticator medicoAuthenticator, SecretariaAuthenticator secretariaAuthenticator, AdminAuthenticator adminAuthenticator)
    {
        _authComposite = new AuthComposite();
        _authComposite.Add(medicoAuthenticator);
        _authComposite.Add(pacienteAuthenticator);
        _authComposite.Add(secretariaAuthenticator);
        _authComposite.Add(adminAuthenticator);

    }
    
    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        return await _authComposite.AuthenticateAsync(username, password);
    }
    
    public async Task<AuthResult> RegisterAsync(RegisterDTO model)
    {
        return await _authComposite.RegisterAsync(model);
    }
}