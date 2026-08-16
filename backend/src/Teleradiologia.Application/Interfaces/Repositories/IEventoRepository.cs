using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Eventos;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IEventoRepository
{
    Task<PagedResult<Evento>> BuscarAsync(FiltroEventos filtro, CancellationToken ct);

    Task<KpisEventosDto> ObtenerKpisAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    Task<List<string>> GetEntidadesAsync(CancellationToken ct);
}
