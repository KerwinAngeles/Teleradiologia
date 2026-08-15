using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Resumen;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IResumenActividadService
{
    Task<BaseResponse<ResumenActividadDto>> ObtenerAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    Task<BaseResponse<int>> EnviarResumenAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);
}
