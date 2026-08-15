using Teleradiologia.Application.Dtos.Account;

namespace Teleradiologia.Application.Abstractions;

// El alta y la autenticación viven en IAccountService / IAuthProvider.
public interface IIdentityService
{
    Task<UsuarioDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<UsuarioDto>> ObtenerTodosAsync(CancellationToken ct);
}
