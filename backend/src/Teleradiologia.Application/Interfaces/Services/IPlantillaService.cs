using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Plantillas;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IPlantillaService
{
    Task<BaseResponse<List<PlantillaDto>>> ListarAsync(Guid radiologoId, string? modalidad, CancellationToken ct);

    Task<BaseResponse<PlantillaDto>> CrearAsync(Guid radiologoId, GuardarPlantillaRequest request, CancellationToken ct);

    Task<BaseResponse<PlantillaDto>> ActualizarAsync(Guid radiologoId, Guid id, GuardarPlantillaRequest request, CancellationToken ct);

    Task<BaseResponse<bool>> EliminarAsync(Guid radiologoId, Guid id, CancellationToken ct);

    // Devuelve el texto ya compuesto y suma uso, para ordenar por frecuencia.
    Task<BaseResponse<string>> AplicarAsync(Guid radiologoId, Guid id, CancellationToken ct);
}
