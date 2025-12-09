using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Services.Authentication.Components;

public class AuthService
{
    private readonly AuthComposite _authComposite;
    
    public AuthService(AppDbContext context)
    {
        var hashService = new PasswordHashService();
        var roleKeyValidationService = new RoleKeyValidationService(context,hashService);
        
        _authComposite = new AuthComposite();
        _authComposite.Add(new MedicoAuthenticator(context, hashService,roleKeyValidationService));
        _authComposite.Add(new PacienteAuthenticator(context, hashService));
        _authComposite.Add(new SecretariaAuthenticator(context, hashService,roleKeyValidationService));
        _authComposite.Add(new AdminAuthenticator(context, hashService,roleKeyValidationService));
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