using MedCenter.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MedCenter.Data;
using MedCenter.Models;
using MedCenter.Extensions;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services;
using MedCenter.Services.Authentication;
using MedCenter.Services.TurnoSv;

public class AuthController : Controller
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;
    private readonly PasswordRecoveryService _passwordRecoveryService;

    public AuthController(AuthService authService, AppDbContext context, PasswordRecoveryService passwordRecoveryService)
    {
        _authService = authService;
        _context = context;
        _passwordRecoveryService = passwordRecoveryService;
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
                new Claim(ClaimTypes.Role, result.Role!.ToString()),
                new Claim(ClaimTypes.Name, result.UserName)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // Iniciar sesión con cookies
            await HttpContext.SignInAsync("Cookies", principal);

            // Guardar también en sesión si querés
            HttpContext.Session.SetInt32("UserId", result.UserId.Value);
            HttpContext.Session.SetString("UserRole", result.Role!.ToString());
            HttpContext.Session.SetString("UserEmail", result.UserName!);

            // Close any existing open sessions for this user (prevent multiple simultaneous sessions)
            var existingSessions = await _context.trazabilidadLogins
                .Where(tl => tl.UsuarioId == result.UserId.Value && tl.MomentoLogout == null)
                .ToListAsync();
            
            foreach (var session in existingSessions)
            {
                session.MomentoLogout = DateTime.UtcNow;
                session.TipoLogout = TipoLogout.Forzado;  // Forced logout due to new login
            }

            // Create new session
            _context.trazabilidadLogins.Add(new TrazabilidadLogin
            {
                UsuarioNombre = result.UserName,
                UsuarioId = result.UserId.Value,
                UsuarioRol = result.Role.Value.ToString().ToRolUsuario(),
                MomentoLogin = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Redirigir según rol
            return result.Role.Value.ToString().ToRolUsuario() switch
            {
                RolUsuario.Medico => RedirectToAction("Dashboard", "Medico"),
                RolUsuario.Secretaria => RedirectToAction("Dashboard", "Secretaria"),
                RolUsuario.Paciente => RedirectToAction("Dashboard", "Paciente"),
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

    // GET: /Auth/RecuperarPassword - Step 1: Email entry
    [HttpGet]
    public IActionResult RecuperarPassword()
    {
        return View();
    }

    // POST: /Auth/RecuperarPassword - Step 1: Send code to email
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Por favor ingresa tu correo electrónico.";
            return View();
        }

        var result = await _passwordRecoveryService.SendRecoveryCodeAsync(email);
        
        if (!result)
        {
            TempData["ErrorMessage"] = "Error al enviar el correo. Por favor intenta más tarde.";
            return View();
        }

        // Always show success (don't reveal if email exists)
        TempData["SuccessMessage"] = "Si el correo existe, recibirás un código de recuperación.";
        TempData["RecoveryEmail"] = email;
        
        return RedirectToAction("IngresarCodigo");
    }

    // GET: /Auth/IngresarCodigo - Step 2: Code entry page
    [HttpGet]
    public IActionResult IngresarCodigo()
    {
        var email = TempData["RecoveryEmail"] as string;
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("RecuperarPassword");
        }

        ViewBag.Email = email;
        ViewBag.MaskedEmail = _passwordRecoveryService.MaskEmail(email);
        TempData.Keep("RecoveryEmail"); // Keep for next request
        return View();
    }

    // POST: /Auth/ValidarCodigo - Step 2: Validate code
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ValidarCodigo(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Código inválido.";
            TempData["RecoveryEmail"] = email;
            return RedirectToAction("IngresarCodigo");
        }

        if (_passwordRecoveryService.ValidateRecoveryCode(email, code, out int userId, out string? errorMessage))
        {
            TempData["RecoveryEmail"] = email;
            TempData["RecoveryCode"] = code;
            return RedirectToAction("RestablecerPassword");
        }

        TempData["ErrorMessage"] = errorMessage ?? "Código inválido o expirado.";
        TempData["RecoveryEmail"] = email;
        return RedirectToAction("IngresarCodigo");
    }

    // GET: /Auth/RestablecerPassword - Step 3: New password form
    [HttpGet]
    public IActionResult RestablecerPassword()
    {
        var email = TempData["RecoveryEmail"] as string;
        var code = TempData["RecoveryCode"] as string;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return RedirectToAction("RecuperarPassword");
        }

        ViewBag.Email = email;
        ViewBag.Code = code;
        TempData.Keep("RecoveryEmail"); // Keep for POST
        TempData.Keep("RecoveryCode"); // Keep for POST
        return View();
    }

    // POST: /Auth/RestablecerPassword - Step 3: Update password
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerPassword(string email, string code, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "La contraseña debe tener al menos 6 caracteres.";
            TempData["RecoveryEmail"] = email;
            TempData["RecoveryCode"] = code;
            return RedirectToAction("RestablecerPassword");
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Las contraseñas no coinciden.";
            TempData["RecoveryEmail"] = email;
            TempData["RecoveryCode"] = code;
            return RedirectToAction("RestablecerPassword");
        }

        if (await _passwordRecoveryService.ResetPasswordAsync(email, code, newPassword))
        {
            TempData["SuccessMessage"] = "Contraseña restablecida exitosamente. Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        TempData["ErrorMessage"] = "No se pudo restablecer la contraseña. El código puede haber expirado.";
        return RedirectToAction("RecuperarPassword");
    }
    
    [HttpPost]
    public async Task<IActionResult> IngresarCodigoRecuperacion(int codigoIngresado,string mail)
    {
        // This method is deprecated - use ValidarCodigo instead
        return RedirectToAction("RecuperarPassword");
    }
    
    // POST: /Auth/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await LogoutInternalAsync(TipoLogout.Manual);
        return RedirectToAction("Login");
    }

    // POST: /Auth/TimeoutLogout
    [HttpPost]
    public async Task<IActionResult> TimeoutLogout()
    {
        await LogoutInternalAsync(TipoLogout.Timeout);
        return Ok(new { success = true });
    }

    private async Task LogoutInternalAsync(TipoLogout tipoLogout)
    {
        var userIdClaim = HttpContext.Session.GetInt32("UserId");
        
        if (userIdClaim.HasValue)
        {
            var entity = await _context.trazabilidadLogins
                .Where(tl => tl.UsuarioId == userIdClaim && tl.MomentoLogout == null)
                .FirstOrDefaultAsync();

            if (entity != null)
            {
                entity.MomentoLogout = DateTime.UtcNow;
                entity.TipoLogout = tipoLogout;
                await _context.SaveChangesAsync();
            }
        }

        await HttpContext.SignOutAsync("Cookies");
        HttpContext.Session.Clear();
    }
}