using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Teleradiologia.Api.Realtime;

// El hub no expone métodos: el servidor empuja, el cliente solo escucha.
[Authorize]
public class NotificacionesHub : Hub
{
    public const string Ruta = "/hubs/notificaciones";

    public const string EventoNotificacion = "notificacion";
}
