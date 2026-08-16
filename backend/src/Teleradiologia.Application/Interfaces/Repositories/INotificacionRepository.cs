using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Notificaciones;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface INotificacionRepository
{
    // Todas las consultas van acotadas al destinatario: una notificación es de una sola persona.
    Task<PagedResult<Notificacion>> BuscarAsync(Guid usuarioId, FiltroNotificaciones filtro, CancellationToken ct);

    Task<List<Notificacion>> GetRecientesAsync(Guid usuarioId, int cantidad, CancellationToken ct);

    Task<int> ContarNoLeidasAsync(Guid usuarioId, CancellationToken ct);

    Task<Notificacion?> GetByIdAsync(Guid usuarioId, Guid id, CancellationToken ct);

    Task<int> MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct);

    void AddRange(IEnumerable<Notificacion> notificaciones);
}
