using Teleradiologia.Application.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Dtos.Account;

public record FiltroUsuarios : PageParams
{
    public EstadoAcceso? Estado { get; init; }

    public RolUsuario? Rol { get; init; }

    // Nombre o email.
    public string? Texto { get; init; }
}
