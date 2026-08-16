using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Estudios;
using Teleradiologia.Application.Informes;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[ApiController]
[Route("api/estudios")]
[Authorize]
public class EstudiosController(IEstudioService estudioService, IInformeService informeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FiltroEstudios filtro, CancellationToken ct)
    {
        // `asignadoAMi` se resuelve acá: el filtro no debe poder pedir los de otro radiólogo.
        var aplicado = filtro.AsignadoAMi
            ? filtro with { RadiologoAsignadoId = ObtenerUsuarioId() }
            : filtro with { RadiologoAsignadoId = null };

        return Ok(await estudioService.BuscarAsync(aplicado, ct));
    }

    [HttpGet("estadisticas")]
    public async Task<IActionResult> Estadisticas(CancellationToken ct) =>
        Ok(await estudioService.ObtenerEstadisticasAsync(ct));

    [HttpPost]
    [Authorize(Roles = $"{nameof(RolUsuario.Tecnico)},{nameof(RolUsuario.Admin)}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(209_715_200)] // ~200MB — un estudio puede traer cientos de slices
    public async Task<IActionResult> Subir([FromForm] SubirEstudioForm form, CancellationToken ct)
    {
        var archivos = new List<byte[]>(form.Archivos.Count);
        foreach (var archivo in form.Archivos)
        {
            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream, ct);
            archivos.Add(stream.ToArray());
        }

        var resultado = await estudioService.SubirEstudioAsync(
            new SubirEstudioRequest(archivos, form.HospitalId, form.Prioridad, ObtenerUsuarioId()), ct);

        return resultado.CreadoAhora
            ? CreatedAtAction(nameof(GetAll), resultado.Estudio)
            : Ok(resultado.Estudio);
    }

    [HttpPost("{id:guid}/tomar")]
    [Authorize(Roles = nameof(RolUsuario.Radiologo))]
    public async Task<IActionResult> Tomar(Guid id, CancellationToken ct) =>
        Ok(await estudioService.TomarEstudioAsync(id, ObtenerUsuarioId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await estudioService.ObtenerPorIdAsync(id, ct));

    [HttpGet("{id:guid}/imagenes")]
    public async Task<IActionResult> GetImagenes(Guid id, CancellationToken ct) =>
        Ok(await estudioService.ObtenerImagenesAsync(id, ObtenerUsuarioId(), ct));

    [HttpGet("{id:guid}/imagenes/{orthancInstanceId}")]
    public async Task<IActionResult> GetImagen(Guid id, string orthancInstanceId, CancellationToken ct)
    {
        var (bytes, contentType) = await estudioService.ObtenerImagenAsync(id, orthancInstanceId, ct);
        return File(bytes, contentType);
    }

    [HttpGet("{id:guid}/imagenes/{orthancInstanceId}/dicom")]
    public async Task<IActionResult> GetArchivoDicom(Guid id, string orthancInstanceId, CancellationToken ct)
    {
        var bytes = await estudioService.ObtenerArchivoDicomAsync(id, orthancInstanceId, ct);
        return File(bytes, "application/dicom");
    }

    [HttpGet("{estudioId:guid}/informes")]
    public async Task<IActionResult> GetInformes(Guid estudioId, CancellationToken ct) =>
        Ok(await informeService.GetByEstudioAsync(estudioId, ct));

    [HttpPost("{estudioId:guid}/informes")]
    [Authorize(Roles = nameof(RolUsuario.Radiologo))]
    public async Task<IActionResult> CrearInforme(Guid estudioId, CrearInformeRequest request, CancellationToken ct)
    {
        var informe = await informeService.CrearAsync(estudioId, ObtenerUsuarioId(), request, ct);
        return CreatedAtAction(nameof(GetInformes), new { estudioId }, informe);
    }

    private Guid ObtenerUsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
