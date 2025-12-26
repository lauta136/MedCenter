using System.Security.Claims;
using MedCenter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiredPermission : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permissionName;

    public RequiredPermission(string permissionName)
    {
        _permissionName = permissionName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Access", null);
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Access", null);
            return;
        }

        // Get database context from DI
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        
        // Check if user has the required permission
        var hasPermission = await dbContext.personaPermisos
            .Include(pp => pp.Permiso)
            .AnyAsync(pp => pp.PersonaId == userId && pp.Permiso.Nombre == _permissionName);

        if (!hasPermission)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Access", null);
        }
    }
}