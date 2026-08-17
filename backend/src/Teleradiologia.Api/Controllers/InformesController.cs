using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Informes;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[ApiController]
[Route("api/informes")]
// El rol va por método: escribir es solo del radiólogo, pero leer y verificar los
// alcanza a los tres roles con distinto alcance. Cuando el rol estaba en la clase, la
// verificación de firma quedaba vedada al técnico pese al comentario que decía lo contrario.
[Authorize]
public class InformesController(IInformeService informeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] FiltroInformes filtro, CancellationToken ct) =>
        Ok(await informeService.BuscarAsync(AplicarAlcance(filtro), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerParaLectura(Guid id, CancellationToken ct) =>
        Ok(await informeService.ObtenerParaLecturaAsync(id, AplicarAlcance(new FiltroInformes()), ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(RolUsuario.Radiologo))]
    public async Task<IActionResult> Editar(Guid id, EditarInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.EditarAsync(id, ObtenerUsuarioId(), request, ct));

    // Cualquier autenticado puede verificar: el punto de una firma es que se pueda comprobar.
    [HttpGet("{id:guid}/verificacion")]
    public async Task<IActionResult> Verificar(Guid id, CancellationToken ct) =>
        Ok(await informeService.VerificarFirmaAsync(id, ct));

    [HttpPost("{id:guid}/firmar")]
    [Authorize(Roles = nameof(RolUsuario.Radiologo))]
    public async Task<IActionResult> Firmar(Guid id, FirmarInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.FirmarAsync(id, ObtenerUsuarioId(), request, ct));

    [HttpPost("{id:guid}/adenda")]
    [Authorize(Roles = nameof(RolUsuario.Radiologo))]
    public async Task<IActionResult> CrearAdenda(Guid id, CrearInformeRequest request, CancellationToken ct) =>
        Ok(await informeService.CrearAdendaAsync(id, ObtenerUsuarioId(), request, ct));

    // Quién ve qué no se negocia desde el cliente: los campos de alcance se reescriben
    // siempre, vengan o no en la query string.
    private FiltroInformes AplicarAlcance(FiltroInformes filtro)
    {
        if (User.IsInRole(nameof(RolUsuario.Admin)))
        {
            return filtro with { RadiologoId = null, SubidoPorId = null };
        }

        var usuarioId = ObtenerUsuarioId();

        return User.IsInRole(nameof(RolUsuario.Radiologo))
            ? filtro with { RadiologoId = usuarioId, SubidoPorId = null }
            : filtro with { RadiologoId = null, SubidoPorId = usuarioId };
    }

    private Guid ObtenerUsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
