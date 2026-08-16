using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Dtos.Eventos;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[Route("api/eventos")]
[Authorize(Roles = nameof(RolUsuario.Admin))]
public class EventosController(IEventoService eventoService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Buscar([FromQuery] FiltroEventos filtro, CancellationToken ct) =>
        Resultado(await eventoService.BuscarAsync(filtro, ct));

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis([FromQuery] int dias = 7, CancellationToken ct = default) =>
        Resultado(await eventoService.ObtenerKpisAsync(dias, ct));

    [HttpGet("entidades")]
    public async Task<IActionResult> Entidades(CancellationToken ct) =>
        Resultado(await eventoService.ListarEntidadesAsync(ct));
}
