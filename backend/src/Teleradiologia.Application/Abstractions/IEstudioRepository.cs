using Teleradiologia.Application.Common;
using Teleradiologia.Application.Estudios;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IEstudioRepository
{
    // Acotado al hospital: el mismo estudio en otro hospital es otro registro, no un duplicado.
    Task<Estudio?> GetExistenteAsync(Guid hospitalId, string orthancStudyId, string studyInstanceUid, CancellationToken ct);

    Task<Estudio?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<Estudio>> BuscarAsync(FiltroEstudios filtro, CancellationToken ct);

    Task<List<EstudioEstadisticaDto>> ProyectarEstadisticasAsync(CancellationToken ct);

    void Add(Estudio estudio);
}
