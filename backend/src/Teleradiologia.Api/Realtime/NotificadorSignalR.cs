using Microsoft.AspNetCore.SignalR;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Dtos.Notificaciones;

namespace Teleradiologia.Api.Realtime;

public class NotificadorSignalR(
    IHubContext<NotificacionesHub> hub,
    ILogger<NotificadorSignalR> logger) : INotificadorTiempoReal
{
    public async Task EnviarAsync(IReadOnlyCollection<Guid> usuarioIds, NotificacionDto notificacion, CancellationToken ct)
    {
        if (usuarioIds.Count == 0)
        {
            return;
        }

        try
        {
            var destinatarios = usuarioIds.Select(id => id.ToString()).ToList();
            await hub.Clients.Users(destinatarios).SendAsync(NotificacionesHub.EventoNotificacion, notificacion, ct);
        }
        catch (Exception ex)
        {
            // El aviso en vivo es best-effort: la notificación ya está guardada.
            logger.LogError(ex, "No se pudo emitir la notificación en tiempo real.");
        }
    }
}
