using Microsoft.AspNetCore.Mvc;

namespace MedCenter.Controllers;
public class AccessController : BaseController
{
    public IActionResult AccessDenied()
    {
        return View("~/Views/Shared/AccessDenied.cshtml");
    }
    
    public IActionResult AccountDeactivated()
    {
        return View("~/Views/Shared/AccountDeactivated.cshtml");
    }
}