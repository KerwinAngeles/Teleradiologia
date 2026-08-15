using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Abstractions;

public interface IEstudioRepository
{
    Task<Estudio?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken ct);

    Task<Estudio?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Estudio>> GetAllAsync(EstadoEstudio? estado, Guid? radiologoAsignadoId, CancellationToken ct);

    void Add(Estudio estudio);
}
