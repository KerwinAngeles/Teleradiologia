using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Eventos;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IEventoService
{
    Task<BaseResponse<PagedResult<EventoDto>>> BuscarAsync(FiltroEventos filtro, CancellationToken ct);

    Task<BaseResponse<KpisEventosDto>> ObtenerKpisAsync(int dias, CancellationToken ct);

    Task<BaseResponse<List<string>>> ListarEntidadesAsync(CancellationToken ct);
}
