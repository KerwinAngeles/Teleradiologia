using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Dtos.Notificaciones;
using Teleradiologia.Application.Interfaces.Services;

namespace Teleradiologia.Api.Controllers;

[Route("api/notificaciones")]
public class NotificacionesController(INotificacionService notificacionService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] FiltroNotificaciones filtro, CancellationToken ct) =>
        Resultado(await notificacionService.BuscarAsync(UsuarioId, filtro, ct));

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct) =>
        Resultado(await notificacionService.ObtenerResumenAsync(UsuarioId, ct));

    [HttpPost("{id:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct) =>
        Resultado(await notificacionService.MarcarLeidaAsync(UsuarioId, id, ct));

    [HttpPost("leidas")]
    public async Task<IActionResult> MarcarTodasLeidas(CancellationToken ct) =>
        Resultado(await notificacionService.MarcarTodasLeidasAsync(UsuarioId, ct));
}
