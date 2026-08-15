using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IGenericRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);

    Task<Usuario?> GetByProveedorUserIdAsync(string proveedorUserId, CancellationToken ct);

    Task<List<Usuario>> GetByEstadoAsync(EstadoAcceso? estado, CancellationToken ct);

    Task<List<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);
}
