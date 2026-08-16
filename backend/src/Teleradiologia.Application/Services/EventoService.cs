using AutoMapper;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Eventos;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;

namespace Teleradiologia.Application.Services;

public class EventoService(IEventoRepository eventoRepository, IMapper mapper) : IEventoService
{
    private const int DiasMaximos = 365;

    public async Task<BaseResponse<PagedResult<EventoDto>>> BuscarAsync(FiltroEventos filtro, CancellationToken ct)
    {
        var pagina = await eventoRepository.BuscarAsync(filtro, ct);

        return BaseResponse<PagedResult<EventoDto>>.Success(new PagedResult<EventoDto>(
            mapper.Map<List<EventoDto>>(pagina.Items),
            pagina.PageNumber,
            pagina.PageSize,
            pagina.TotalCount));
    }

    public async Task<BaseResponse<KpisEventosDto>> ObtenerKpisAsync(int dias, CancellationToken ct)
    {
        var ventana = Math.Clamp(dias, 1, DiasMaximos);
        var hasta = DateTimeOffset.UtcNow;

        return BaseResponse<KpisEventosDto>.Success(
            await eventoRepository.ObtenerKpisAsync(hasta.AddDays(-ventana), hasta, ct));
    }

    public async Task<BaseResponse<List<string>>> ListarEntidadesAsync(CancellationToken ct) =>
        BaseResponse<List<string>>.Success(await eventoRepository.GetEntidadesAsync(ct));
}
