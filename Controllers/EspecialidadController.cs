
using MedCenter.Services.EspecialidadService;
using Microsoft.AspNetCore.Mvc;

namespace MedCenter.Controllers;
public class EspecialidadController : BaseController
{
    private readonly EspecialidadService e;

    public EspecialidadController(EspecialidadService especialidadService)
    {
        e = especialidadService;
    }

    public async Task<IActionResult> GetEspecialidadesJson()
    {
        var especialidades = await e.GetEspecialidades();
        return Json(especialidades);
    }
}