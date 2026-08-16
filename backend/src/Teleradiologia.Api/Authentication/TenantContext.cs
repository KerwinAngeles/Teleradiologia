using System.Security.Claims;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Identity.Authentication;

namespace Teleradiologia.Api.Authentication;

// Los hospitales habilitados vienen en los claims que arma UsuarioClaimsTransformation, así no
// se consulta la base en cada request para resolver el alcance.
public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private ClaimsPrincipal? _principalCacheado;
    private HashSet<Guid> _hospitales = [];
    private bool _veTodo;

    public bool VeTodosLosHospitales
    {
        get
        {
            Refrescar();
            return _veTodo;
        }
    }

    public IReadOnlyCollection<Guid> HospitalesPermitidos
    {
        get
        {
            Refrescar();
            return _hospitales;
        }
    }

    public bool PuedeVer(Guid hospitalId) => VeTodosLosHospitales || HospitalesPermitidos.Contains(hospitalId);

    // El principal se lee en cada acceso, no en el constructor: este servicio se instancia
    // durante UsuarioClaimsTransformation, cuando HttpContext.User todavía es el token crudo
    // y no tiene ni el rol ni los hospitales.
    private void Refrescar()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (ReferenceEquals(principal, _principalCacheado))
        {
            return;
        }

        _principalCacheado = principal;
        _veTodo = principal?.IsInRole(nameof(RolUsuario.Admin)) ?? false;

        _hospitales = principal is null
            ? []
            : [.. principal.FindAll(ClaimsLocales.HospitalId)
                .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)];
    }
}
