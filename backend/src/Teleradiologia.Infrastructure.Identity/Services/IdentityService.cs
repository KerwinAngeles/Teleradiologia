using AutoMapper;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Application.Interfaces.Repositories;

namespace Teleradiologia.Infrastructure.Identity.Services;

public class IdentityService(IUsuarioRepository usuarioRepository, IMapper mapper) : IIdentityService
{
    public async Task<UsuarioDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct)
    {
        var usuario = await usuarioRepository.GetByIdAsync(id, ct);
        return usuario is null ? null : mapper.Map<UsuarioDto>(usuario);
    }

    public async Task<IReadOnlyList<UsuarioDto>> ObtenerTodosAsync(CancellationToken ct)
    {
        var usuarios = await usuarioRepository.GetByEstadoAsync(null, ct);
        return mapper.Map<List<UsuarioDto>>(usuarios);
    }
}
