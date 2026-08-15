using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[Route("api/resumen")]
[Authorize(Roles = nameof(RolUsuario.Admin))]
public class ResumenController(IResumenActividadService resumenService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int dias = 1, CancellationToken ct = default)
    {
        var hasta = DateTimeOffset.UtcNow;
        return Resultado(await resumenService.ObtenerAsync(hasta.AddDays(-Math.Max(1, dias)), hasta, ct));
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar([FromQuery] int dias = 1, CancellationToken ct = default)
    {
        var hasta = DateTimeOffset.UtcNow;
        return Resultado(await resumenService.EnviarResumenAsync(hasta.AddDays(-Math.Max(1, dias)), hasta, ct));
    }
}
