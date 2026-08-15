using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext db) : GenericRepository<Usuario>(db), IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) =>
        Db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

    public Task<Usuario?> GetByProveedorUserIdAsync(string proveedorUserId, CancellationToken ct) =>
        Db.Usuarios.FirstOrDefaultAsync(u => u.ProveedorUserId == proveedorUserId, ct);

    public Task<List<Usuario>> GetByEstadoAsync(EstadoAcceso? estado, CancellationToken ct)
    {
        var query = Db.Usuarios.AsQueryable();

        if (estado is not null)
        {
            query = query.Where(u => u.EstadoAcceso == estado);
        }

        return query.OrderBy(u => u.EstadoAcceso).ThenBy(u => u.NombreCompleto).ToListAsync(ct);
    }

    public Task<List<Usuario>> GetByRolAsync(RolUsuario rol, CancellationToken ct) =>
        Db.Usuarios
            .Where(u => u.Rol == rol && u.EstadoAcceso == EstadoAcceso.Aprobado)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Db.Usuarios.AnyAsync(ct);
}
