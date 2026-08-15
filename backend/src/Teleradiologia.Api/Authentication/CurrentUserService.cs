using System.Security.Claims;
using Teleradiologia.Application.Abstractions;

namespace Teleradiologia.Api.Authentication;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UsuarioId =>
        Guid.TryParse(Claim(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Claim(ClaimTypes.Email);

    private string? Claim(string tipo) => httpContextAccessor.HttpContext?.User.FindFirstValue(tipo);
}
