using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Notificaciones;

namespace Teleradiologia.Application.Interfaces.Services;

public interface INotificacionService
{
    Task<BaseResponse<PagedResult<NotificacionDto>>> BuscarAsync(Guid usuarioId, FiltroNotificaciones filtro, CancellationToken ct);

    Task<BaseResponse<ResumenNotificaciones>> ObtenerResumenAsync(Guid usuarioId, CancellationToken ct);

    Task<BaseResponse<bool>> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId, CancellationToken ct);

    Task<BaseResponse<int>> MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct);
}
