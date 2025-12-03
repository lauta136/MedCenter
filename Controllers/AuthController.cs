using MedCenter.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using MedCenter.Data;
using MedCenter.Models;
using MedCenter.Extensions;
using MedCenter.Services.TurnoSv;
using Org.BouncyCastle.Bcpg;
using Microsoft.EntityFrameworkCore;

public class AuthController : Controller
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(AuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
        
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