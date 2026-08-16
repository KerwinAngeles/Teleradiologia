using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Hospitales;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IHospitalRepository : IGenericRepository<Hospital>
{
    Task<List<Hospital>> GetActivosAsync(CancellationToken ct);

    Task<PagedResult<Hospital>> BuscarAsync(FiltroHospitales filtro, CancellationToken ct);

    Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct);

    // El catálogo del MISPAS no está alcanzado por el filtro de inquilino: es referencia pública.
    Task<PagedResult<EstablecimientoCatalogo>> BuscarEnCatalogoAsync(FiltroCatalogo filtro, CancellationToken ct);

    Task<List<string>> GetTiposCatalogoAsync(CancellationToken ct);

    Task<List<string>> GetProvinciasAsync(CancellationToken ct);
}
