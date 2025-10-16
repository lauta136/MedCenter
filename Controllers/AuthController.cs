using MedCenter.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

public class AuthController : Controller
{
    private readonly AuthService _authService;
    
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }
    
    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDTO model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var result = await _authService.LoginAsync(model.Username, model.Password);
        
        if (result.Success)
        {
            // Crear los claims del usuario
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.Value.ToString()),
                new Claim(ClaimTypes.Email, result.UserName!),
                new Claim(ClaimTypes.Role, result.Role!),
                new Claim(ClaimTypes.Name, result.UserName)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // Iniciar sesión con cookies
            await HttpContext.SignInAsync("Cookies", principal);

            // Guardar también en sesión si querés
            HttpContext.Session.SetInt32("UserId", result.UserId.Value);
            HttpContext.Session.SetString("UserRole", result.Role!);
            HttpContext.Session.SetString("UserEmail", result.UserName!);

            // Redirigir según rol
            return result.Role switch
            {
                "Medico" => RedirectToAction("Dashboard", "Medico"),
                "Secretaria" => RedirectToAction("Dashboard", "Secretaria"),
                "Paciente" => RedirectToAction("Dashboard", "Paciente"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(model);
    }
    
    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterDTO());
    }
    
    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDTO model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var result = await _authService.RegisterAsync(model);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Usuario registrado exitosamente. Puede iniciar sesión.";
            return RedirectToAction("Login");
        }
        
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(model);
    }
    
    // POST: /Auth/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}