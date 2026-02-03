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
        // Check if this is an API request (expects JSON response)
        bool isApiRequest = IsApiRequest(context.HttpContext);

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = isApiRequest 
                ? new UnauthorizedObjectResult(new { success = false, message = "No autenticado" })
                : new RedirectToActionResult("AccessDenied", "Access", null);
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            context.Result = isApiRequest 
                ? new UnauthorizedObjectResult(new { success = false, message = "Usuario inválido" })
                : new RedirectToActionResult("AccessDenied", "Access", null);
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
            context.Result = isApiRequest 
                ? new JsonResult(new { success = false, message = $"No tienes permiso para realizar esta acción. Permiso requerido: {_permissionName}" }) 
                  { StatusCode = 403 }
                : new RedirectToActionResult("AccessDenied", "Access", null);
        }
    }

    private bool IsApiRequest(HttpContext context)
    {
        // Check if Content-Type is JSON (for POST/PUT requests with JSON body)
        var contentType = context.Request.ContentType?.ToLower() ?? "";
        if (contentType.Contains("application/json"))
            return true;

        // Check if request accepts JSON
        var acceptHeader = context.Request.Headers["Accept"].ToString().ToLower();
        if (acceptHeader.Contains("application/json") || acceptHeader.Contains("*/*"))
            return true;

        // Check if it's an AJAX request
        if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return true;

        // Check if the request path suggests it's an API endpoint
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/api/"))
            return true;

        // Check if it's a POST/PUT/DELETE/PATCH request (typically API calls)
        var method = context.Request.Method.ToUpper();
        if (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
            return true;

        return false;
    }
}