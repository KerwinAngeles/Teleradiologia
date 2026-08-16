using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Dtos.Hospitales;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[Route("api/hospitales")]
public class HospitalesController(IHospitalService hospitalService) : BaseApiController
{
    // Devuelve solo los hospitales del usuario: el filtro de inquilino lo aplica el DbContext.
    // Sin paginar: lo consume el selector de la subida de estudios.
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Resultado(await hospitalService.ListarAsync(ct));

    [HttpGet("buscar")]
    [Authorize(Roles = nameof(RolUsuario.Admin))]
    public async Task<IActionResult> Buscar([FromQuery] FiltroHospitales filtro, CancellationToken ct) =>
        Resultado(await hospitalService.BuscarAsync(filtro, ct));

    [HttpGet("catalogo")]
    [Authorize(Roles = nameof(RolUsuario.Admin))]
    public async Task<IActionResult> BuscarEnCatalogo([FromQuery] FiltroCatalogo filtro, CancellationToken ct) =>
        Resultado(await hospitalService.BuscarEnCatalogoAsync(filtro, ct));

    [HttpGet("catalogo/tipos")]
    [Authorize(Roles = nameof(RolUsuario.Admin))]
    public async Task<IActionResult> TiposCatalogo(CancellationToken ct) =>
        Resultado(await hospitalService.ListarTiposCatalogoAsync(ct));

    [HttpGet("catalogo/provincias")]
    [Authorize(Roles = nameof(RolUsuario.Admin))]
    public async Task<IActionResult> Provincias(CancellationToken ct) =>
        Resultado(await hospitalService.ListarProvinciasAsync(ct));

    [HttpPost]
    [Authorize(Roles = nameof(RolUsuario.Admin))]
    public async Task<IActionResult> Crear(CrearHospitalRequest request, CancellationToken ct) =>
        Resultado(await hospitalService.CrearAsync(request, ct), h => CreatedAtAction(nameof(GetAll), h));
}
