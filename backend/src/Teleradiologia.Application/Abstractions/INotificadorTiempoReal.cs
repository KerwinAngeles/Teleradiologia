using Teleradiologia.Application.Dtos.Notificaciones;

namespace Teleradiologia.Application.Abstractions;

// Puerto hacia el transporte en tiempo real (hoy SignalR). Application no conoce el hub.
public interface INotificadorTiempoReal
{
    Task EnviarAsync(IReadOnlyCollection<Guid> usuarioIds, NotificacionDto notificacion, CancellationToken ct);
}
