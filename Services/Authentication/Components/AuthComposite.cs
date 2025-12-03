using MedCenter.DTOs;
namespace MedCenter.Services.Authentication.Components
{
    public class AuthComposite : IAuthComponent
    {
        private readonly List<IAuthComponent> _authenticators = new();

        public void Add(IAuthComponent component)
        {
            _authenticators.Add(component);
        }

        public void Remove(IAuthComponent component)
        {
            _authenticators.Remove(component);
        }

        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            foreach (var authenticator in _authenticators)
            {
                var result = await authenticator.AuthenticateAsync(username, password);
                if (result.Success) 
                    return result;
            }
            return new AuthResult { Success = false, ErrorMessage = "Usuario o contraseña incorrectos" };
        }

        public async Task<AuthResult> RegisterAsync(RegisterDTO model)
        {
            foreach (var authenticator in _authenticators)
            {
                var result = await authenticator.RegisterAsync(model);
                if (result.Success)
                    return result;
            }

            return new AuthResult { Success = false, ErrorMessage = "No se pudo registrar el usuario" };
        }
    }
}