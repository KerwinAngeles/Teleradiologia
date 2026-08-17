using Teleradiologia.Application.Common;
using Teleradiologia.Application.Informes;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IInformeRepository
{
    Task<Informe?> GetByIdAsync(Guid id, CancellationToken ct);

    // Con el estudio, el paciente y el hospital cargados: el listado y la hoja los muestran.
    Task<Informe?> GetConEstudioAsync(Guid id, CancellationToken ct);

    Task<PagedResult<Informe>> BuscarAsync(FiltroInformes filtro, CancellationToken ct);

    Task<bool> ExisteParaEstudioAsync(Guid estudioId, CancellationToken ct);

    Task<IReadOnlyList<Informe>> GetByEstudioAsync(Guid estudioId, CancellationToken ct);

    void Add(Informe informe);
}
