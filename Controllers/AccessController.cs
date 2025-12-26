using Microsoft.AspNetCore.Mvc;

namespace MedCenter.Controllers;
public class AccessController : BaseController
{
    public IActionResult AccessDenied()
    {
        return View("~/Views/Shared/AccessDenied.cshtml");
    }
}