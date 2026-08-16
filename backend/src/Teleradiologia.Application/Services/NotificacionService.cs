using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Notificaciones;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Services;

public class NotificacionService(
    INotificacionRepository notificacionRepository,
    IUnitOfWork unitOfWork) : INotificacionService
{
    private const int RecientesEnElPanel = 8;

    public async Task<BaseResponse<PagedResult<NotificacionDto>>> BuscarAsync(Guid usuarioId, FiltroNotificaciones filtro, CancellationToken ct)
    {
        var pagina = await notificacionRepository.BuscarAsync(usuarioId, filtro, ct);

        return BaseResponse<PagedResult<NotificacionDto>>.Success(new PagedResult<NotificacionDto>(
            [.. pagina.Items.Select(Mapear)],
            pagina.PageNumber,
            pagina.PageSize,
            pagina.TotalCount));
    }

    public async Task<BaseResponse<ResumenNotificaciones>> ObtenerResumenAsync(Guid usuarioId, CancellationToken ct)
    {
        var noLeidas = await notificacionRepository.ContarNoLeidasAsync(usuarioId, ct);
        var recientes = await notificacionRepository.GetRecientesAsync(usuarioId, RecientesEnElPanel, ct);

        return BaseResponse<ResumenNotificaciones>.Success(
            new ResumenNotificaciones(noLeidas, [.. recientes.Select(Mapear)]));
    }

    public async Task<BaseResponse<bool>> MarcarLeidaAsync(Guid usuarioId, Guid notificacionId, CancellationToken ct)
    {
        var notificacion = await notificacionRepository.GetByIdAsync(usuarioId, notificacionId, ct);
        if (notificacion is null)
        {
            return BaseResponse<bool>.Fail("No existe la notificación.", ErrorCode.NoEncontrado);
        }

        if (notificacion.LeidaAt is null)
        {
            notificacion.LeidaAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return BaseResponse<bool>.Success(true);
    }

    public async Task<BaseResponse<int>> MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct) =>
        BaseResponse<int>.Success(await notificacionRepository.MarcarTodasLeidasAsync(usuarioId, ct));

    public static NotificacionDto Mapear(Notificacion n) => new(
        n.Id,
        n.Tipo,
        n.Titulo,
        n.Mensaje,
        n.EstudioId,
        n.Estudio?.Paciente?.NombreCompleto,
        n.Estudio?.Modalidad,
        n.Estudio?.Hospital?.Nombre,
        n.Estudio?.Prioridad,
        n.LeidaAt,
        n.CreatedAt);
}
