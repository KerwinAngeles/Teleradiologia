using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Dtos.Plantillas;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[Route("api/plantillas")]
[Authorize(Roles = nameof(RolUsuario.Radiologo))]
public class PlantillasController(IPlantillaService plantillaService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? modalidad, CancellationToken ct) =>
        Resultado(await plantillaService.ListarAsync(UsuarioId, modalidad, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarPlantillaRequest request, CancellationToken ct) =>
        Resultado(await plantillaService.CrearAsync(UsuarioId, request, ct), p => CreatedAtAction(nameof(Listar), p));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, GuardarPlantillaRequest request, CancellationToken ct) =>
        Resultado(await plantillaService.ActualizarAsync(UsuarioId, id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) =>
        Resultado(await plantillaService.EliminarAsync(UsuarioId, id, ct));

    [HttpPost("{id:guid}/aplicar")]
    public async Task<IActionResult> Aplicar(Guid id, CancellationToken ct) =>
        Resultado(await plantillaService.AplicarAsync(UsuarioId, id, ct));
}
