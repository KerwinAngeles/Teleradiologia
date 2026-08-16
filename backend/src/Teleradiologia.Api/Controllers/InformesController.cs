using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Informes;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[ApiController]
[Route("api/informes")]
[Authorize(Roles = nameof(RolUsuario.Radiologo))]
public class InformesController(IInformeService informeService) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, EditarInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.EditarAsync(id, ObtenerUsuarioId(), request, ct));

    // Cualquier autenticado puede verificar: el punto de una firma es que se pueda comprobar.
    [HttpGet("{id:guid}/verificacion")]
    [Authorize]
    public async Task<IActionResult> Verificar(Guid id, CancellationToken ct) =>
        Ok(await informeService.VerificarFirmaAsync(id, ct));

    [HttpPost("{id:guid}/firmar")]
    public async Task<IActionResult> Firmar(Guid id, FirmarInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.FirmarAsync(id, ObtenerUsuarioId(), request, ct));

    [HttpPost("{id:guid}/adenda")]
    public async Task<IActionResult> CrearAdenda(Guid id, CrearInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.CrearAdendaAsync(id, ObtenerUsuarioId(), request, ct));

    private Guid ObtenerUsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
