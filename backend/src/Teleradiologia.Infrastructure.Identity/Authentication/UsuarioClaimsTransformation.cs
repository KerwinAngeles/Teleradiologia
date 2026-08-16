using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Teleradiologia.Application.Interfaces.Repositories;

namespace Teleradiologia.Infrastructure.Identity.Authentication;

// Cambia la identidad del token (sub de Supabase, role=authenticated) por la local:
// el Id de nuestra tabla Usuarios y el rol Tecnico/Radiologo/Admin.
public class UsuarioClaimsTransformation(
    IUsuarioRepository usuarioRepository,
    ILogger<UsuarioClaimsTransformation> logger) : IClaimsTransformation
{
    private static readonly string[] ClaimsReemplazados =
    [
        ClaimTypes.NameIdentifier,
        ClaimTypes.Role,
        ClaimTypes.Name,
        ClaimTypes.Email,
        "sub",
        "role",
    ];

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Se invoca más de una vez por request; sin este corte se consultaría la base de más.
        if (principal.HasClaim(c => c.Type == ClaimsLocales.UsuarioId))
        {
            return principal;
        }

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub))
        {
            return principal;
        }

        var usuario = await usuarioRepository.GetByProveedorUserIdAsync(sub, CancellationToken.None);

        if (usuario is null)
        {
            logger.LogWarning("Token válido sin perfil local para el sub {Sub}", sub);
            return principal;
        }

        // Corta el acceso sin esperar a que expire el token: la política Fallback exige UsuarioId.
        if (!usuario.PuedeIniciarSesion)
        {
            return principal;
        }

        // Una sola identidad: con dos, FindFirstValue devolvería el sub de Supabase.
        var identidad = new ClaimsIdentity(
            principal.Claims.Where(c => !ClaimsReemplazados.Contains(c.Type)),
            principal.Identity.AuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role);

        identidad.AddClaim(new Claim(ClaimsLocales.UsuarioId, usuario.Id.ToString()));
        identidad.AddClaim(new Claim(ClaimsLocales.ProveedorUserId, sub));
        identidad.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()));
        identidad.AddClaim(new Claim(ClaimTypes.Email, usuario.Email));
        identidad.AddClaim(new Claim(ClaimTypes.Name, usuario.NombreCompleto));
        identidad.AddClaim(new Claim(ClaimTypes.Role, usuario.Rol.ToString()));

        foreach (var habilitacion in usuario.Hospitales)
        {
            identidad.AddClaim(new Claim(ClaimsLocales.HospitalId, habilitacion.HospitalId.ToString()));
        }

        return new ClaimsPrincipal(identidad);
    }
}
