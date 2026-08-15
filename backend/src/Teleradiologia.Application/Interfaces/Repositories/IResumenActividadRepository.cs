using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IResumenActividadRepository
{
    Task<int> ContarEstudiosRecibidosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);

    Task<int> ContarInformesFirmadosAsync(DateTimeOffset desde, DateTimeOffset hasta, bool adendas, CancellationToken ct);

    Task<int> ContarEstudiosPorEstadoAsync(EstadoEstudio estado, CancellationToken ct);

    Task<List<FirmasPorRadiologo>> ContarFirmasPorRadiologoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct);
}

public record FirmasPorRadiologo(Guid RadiologoId, int Firmados);
