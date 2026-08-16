using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IGenericRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);

    Task<Usuario?> GetByProveedorUserIdAsync(string proveedorUserId, CancellationToken ct);

    Task<List<Usuario>> GetByEstadoAsync(EstadoAcceso? estado, CancellationToken ct);

    Task<PagedResult<Usuario>> BuscarAsync(FiltroUsuarios filtro, CancellationToken ct);

    Task<List<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct);

    // Radiólogos aprobados habilitados en ese hospital: los que pueden leer ese estudio.
    Task<List<Usuario>> GetRadiologosDeHospitalAsync(Guid hospitalId, CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);
}
