using Microsoft.AspNetCore.SignalR;
using Teleradiologia.Infrastructure.Identity.Authentication;

namespace Teleradiologia.Api.Realtime;

// SignalR agrupa conexiones por usuario con este id. Tiene que ser el de NUESTRA tabla, no el
// `sub` de Supabase: es el que usa el resto del sistema para dirigir las notificaciones.
public class UsuarioIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(ClaimsLocales.UsuarioId)?.Value;
}
