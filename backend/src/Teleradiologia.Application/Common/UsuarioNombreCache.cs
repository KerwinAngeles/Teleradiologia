using Teleradiologia.Application.Abstractions;

namespace Teleradiologia.Application.Common;

public class UsuarioNombreCache(IIdentityService identityService)
{
    private readonly Dictionary<Guid, string> _cache = [];

    public async Task<string> ObtenerAsync(Guid usuarioId, CancellationToken ct)
    {
        if (_cache.TryGetValue(usuarioId, out var nombre))
        {
            return nombre;
        }

        var usuario = await identityService.ObtenerPorIdAsync(usuarioId, ct);
        nombre = usuario?.NombreCompleto ?? "(usuario eliminado)";
        _cache[usuarioId] = nombre;
        return nombre;
    }
}
