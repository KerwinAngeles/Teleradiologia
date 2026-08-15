using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IInformeRepository
{
    Task<Informe?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteParaEstudioAsync(Guid estudioId, CancellationToken ct);

    Task<IReadOnlyList<Informe>> GetByEstudioAsync(Guid estudioId, CancellationToken ct);

    void Add(Informe informe);
}
