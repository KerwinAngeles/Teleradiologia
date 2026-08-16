using AutoMapper;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Hospitales;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Services;

public class HospitalService(
    IHospitalRepository hospitalRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IHospitalService
{
    public async Task<BaseResponse<List<HospitalDto>>> ListarAsync(CancellationToken ct)
    {
        var hospitales = await hospitalRepository.GetActivosAsync(ct);
        return BaseResponse<List<HospitalDto>>.Success(mapper.Map<List<HospitalDto>>(hospitales));
    }

    public async Task<BaseResponse<PagedResult<HospitalDto>>> BuscarAsync(FiltroHospitales filtro, CancellationToken ct)
    {
        var pagina = await hospitalRepository.BuscarAsync(filtro, ct);

        return BaseResponse<PagedResult<HospitalDto>>.Success(new PagedResult<HospitalDto>(
            mapper.Map<List<HospitalDto>>(pagina.Items), pagina.PageNumber, pagina.PageSize, pagina.TotalCount));
    }

    public async Task<BaseResponse<PagedResult<EstablecimientoCatalogoDto>>> BuscarEnCatalogoAsync(FiltroCatalogo filtro, CancellationToken ct)
    {
        var pagina = await hospitalRepository.BuscarEnCatalogoAsync(filtro, ct);

        return BaseResponse<PagedResult<EstablecimientoCatalogoDto>>.Success(new PagedResult<EstablecimientoCatalogoDto>(
            mapper.Map<List<EstablecimientoCatalogoDto>>(pagina.Items),
            pagina.PageNumber,
            pagina.PageSize,
            pagina.TotalCount));
    }

    public async Task<BaseResponse<List<string>>> ListarTiposCatalogoAsync(CancellationToken ct) =>
        BaseResponse<List<string>>.Success(await hospitalRepository.GetTiposCatalogoAsync(ct));

    public async Task<BaseResponse<List<string>>> ListarProvinciasAsync(CancellationToken ct) =>
        BaseResponse<List<string>>.Success(await hospitalRepository.GetProvinciasAsync(ct));

    public async Task<BaseResponse<HospitalDto>> CrearAsync(CrearHospitalRequest request, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim();

        if (await hospitalRepository.ExisteNombreAsync(nombre, ct))
        {
            return BaseResponse<HospitalDto>.Fail("Ya existe un hospital con ese nombre.", ErrorCode.Conflicto);
        }

        var hospital = new Hospital
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            CodigoExterno = request.CodigoExterno,
            Provincia = request.Provincia?.Trim(),
            Municipio = request.Municipio?.Trim(),
            EmailContacto = request.EmailContacto?.Trim(),
            SlaStatMinutos = request.SlaStatMinutos,
            SlaUrgenteMinutos = request.SlaUrgenteMinutos,
            SlaRutinaMinutos = request.SlaRutinaMinutos,
            Activo = true,
        };

        hospitalRepository.Add(hospital);
        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<HospitalDto>.Success(mapper.Map<HospitalDto>(hospital));
    }
}
