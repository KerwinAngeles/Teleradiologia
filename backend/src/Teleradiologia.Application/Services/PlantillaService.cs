using System.Text;
using System.Text.Json;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Plantillas;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Services;

public class PlantillaService(
    IPlantillaRepository plantillaRepository,
    IUnitOfWork unitOfWork) : IPlantillaService
{
    private const int MaximoSecciones = 20;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<BaseResponse<List<PlantillaDto>>> ListarAsync(Guid radiologoId, string? modalidad, CancellationToken ct)
    {
        var plantillas = await plantillaRepository.GetDelRadiologoAsync(radiologoId, modalidad, ct);
        return BaseResponse<List<PlantillaDto>>.Success([.. plantillas.Select(Mapear)]);
    }

    public async Task<BaseResponse<PlantillaDto>> CrearAsync(Guid radiologoId, GuardarPlantillaRequest request, CancellationToken ct)
    {
        var validacion = Validar(request);
        if (validacion is not null)
        {
            return BaseResponse<PlantillaDto>.Fail(validacion, ErrorCode.Invalido);
        }

        var nombre = request.Nombre.Trim();

        if (await plantillaRepository.ExisteNombreAsync(radiologoId, nombre, null, ct))
        {
            return BaseResponse<PlantillaDto>.Fail("Ya tenés una plantilla con ese nombre.", ErrorCode.Conflicto);
        }

        var plantilla = new PlantillaInforme
        {
            Id = Guid.NewGuid(),
            RadiologoId = radiologoId,
            Nombre = nombre,
        };

        Volcar(request, plantilla);

        plantillaRepository.Add(plantilla);
        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<PlantillaDto>.Success(Mapear(plantilla));
    }

    public async Task<BaseResponse<PlantillaDto>> ActualizarAsync(Guid radiologoId, Guid id, GuardarPlantillaRequest request, CancellationToken ct)
    {
        var validacion = Validar(request);
        if (validacion is not null)
        {
            return BaseResponse<PlantillaDto>.Fail(validacion, ErrorCode.Invalido);
        }

        var plantilla = await plantillaRepository.GetByIdAsync(radiologoId, id, ct);
        if (plantilla is null)
        {
            return BaseResponse<PlantillaDto>.Fail("No existe la plantilla.", ErrorCode.NoEncontrado);
        }

        var nombre = request.Nombre.Trim();

        if (await plantillaRepository.ExisteNombreAsync(radiologoId, nombre, id, ct))
        {
            return BaseResponse<PlantillaDto>.Fail("Ya tenés otra plantilla con ese nombre.", ErrorCode.Conflicto);
        }

        plantilla.Nombre = nombre;
        Volcar(request, plantilla);

        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<PlantillaDto>.Success(Mapear(plantilla));
    }

    public async Task<BaseResponse<bool>> EliminarAsync(Guid radiologoId, Guid id, CancellationToken ct)
    {
        var plantilla = await plantillaRepository.GetByIdAsync(radiologoId, id, ct);
        if (plantilla is null)
        {
            return BaseResponse<bool>.Fail("No existe la plantilla.", ErrorCode.NoEncontrado);
        }

        // Baja lógica: se conserva por trazabilidad de los informes que salieron de ella.
        plantilla.Activa = false;
        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<bool>.Success(true);
    }

    public async Task<BaseResponse<string>> AplicarAsync(Guid radiologoId, Guid id, CancellationToken ct)
    {
        var plantilla = await plantillaRepository.GetByIdAsync(radiologoId, id, ct);
        if (plantilla is null)
        {
            return BaseResponse<string>.Fail("No existe la plantilla.", ErrorCode.NoEncontrado);
        }

        plantilla.VecesUsada++;
        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<string>.Success(Componer(Deserializar(plantilla.Secciones)));
    }

    private static string? Validar(GuardarPlantillaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "La plantilla necesita un nombre.";
        }

        if (request.Secciones.Count == 0)
        {
            return "La plantilla necesita al menos una sección.";
        }

        if (request.Secciones.Count > MaximoSecciones)
        {
            return $"Una plantilla no puede tener más de {MaximoSecciones} secciones.";
        }

        return request.Secciones.Any(s => string.IsNullOrWhiteSpace(s.Titulo))
            ? "Todas las secciones necesitan un título."
            : null;
    }

    private static void Volcar(GuardarPlantillaRequest request, PlantillaInforme plantilla)
    {
        plantilla.Modalidad = string.IsNullOrWhiteSpace(request.Modalidad) ? null : request.Modalidad.Trim().ToUpperInvariant();
        plantilla.RegionAnatomica = request.RegionAnatomica?.Trim();
        plantilla.Descripcion = request.Descripcion?.Trim();
        plantilla.Favorita = request.Favorita;

        // El orden se reasigna según la posición recibida: el cliente manda la lista ya ordenada.
        var secciones = request.Secciones
            .Select((s, i) => new SeccionPlantillaDto(s.Titulo.Trim(), s.Contenido?.Trim(), i))
            .ToList();

        plantilla.Secciones = JsonSerializer.Serialize(secciones, Json);
    }

    private static PlantillaDto Mapear(PlantillaInforme p) => new(
        p.Id,
        p.Nombre,
        p.Modalidad,
        p.RegionAnatomica,
        p.Descripcion,
        Deserializar(p.Secciones),
        p.Favorita,
        p.VecesUsada,
        p.CreatedAt);

    private static List<SeccionPlantillaDto> Deserializar(string secciones)
    {
        try
        {
            return JsonSerializer.Deserialize<List<SeccionPlantillaDto>>(secciones, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // El texto compuesto es lo que arranca el informe: título en mayúsculas y el cuerpo debajo.
    private static string Componer(IReadOnlyList<SeccionPlantillaDto> secciones)
    {
        var texto = new StringBuilder();

        foreach (var seccion in secciones.OrderBy(s => s.Orden))
        {
            texto.AppendLine(seccion.Titulo.ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(seccion.Contenido))
            {
                texto.AppendLine(seccion.Contenido);
            }

            texto.AppendLine();
        }

        return texto.ToString().TrimEnd();
    }
}
