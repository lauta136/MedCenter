using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public class BaseController : Controller
{
 protected int? UserId => User.Identity?.IsAuthenticated == true 
        ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0") 
        : null;
        
    protected string? UserRole => User.FindFirst(ClaimTypes.Role)?.Value;
    protected string? UserName => User.FindFirst(ClaimTypes.Name)?.Value;
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    protected bool IsRole(string role) => UserRole == role;
    protected IActionResult RedirectToLogin() => RedirectToAction("Login", "Auth");
}
