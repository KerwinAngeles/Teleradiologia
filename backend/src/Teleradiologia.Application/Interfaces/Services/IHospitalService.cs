using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Hospitales;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IHospitalService
{
    Task<BaseResponse<List<HospitalDto>>> ListarAsync(CancellationToken ct);

    Task<BaseResponse<PagedResult<HospitalDto>>> BuscarAsync(FiltroHospitales filtro, CancellationToken ct);

    Task<BaseResponse<PagedResult<EstablecimientoCatalogoDto>>> BuscarEnCatalogoAsync(FiltroCatalogo filtro, CancellationToken ct);

    Task<BaseResponse<List<string>>> ListarTiposCatalogoAsync(CancellationToken ct);

    Task<BaseResponse<List<string>>> ListarProvinciasAsync(CancellationToken ct);

    Task<BaseResponse<HospitalDto>> CrearAsync(CrearHospitalRequest request, CancellationToken ct);
}
