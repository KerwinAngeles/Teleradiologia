using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Account;
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
        Db.Usuarios
            .Include(u => u.Hospitales)
            .FirstOrDefaultAsync(u => u.ProveedorUserId == proveedorUserId, ct);

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

    public async Task<PagedResult<Usuario>> BuscarAsync(FiltroUsuarios filtro, CancellationToken ct)
    {
        var query = Db.Usuarios.AsQueryable();

        if (filtro.Estado is { } estado)
        {
            query = query.Where(u => u.EstadoAcceso == estado);
        }

        if (filtro.Rol is { } rol)
        {
            query = query.Where(u => u.Rol == rol);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.NombreCompleto, texto) || EF.Functions.ILike(u.Email, texto));
        }

        var total = await query.CountAsync(ct);

        // Los pendientes primero: son los que piden una decisión.
        var items = await query
            .OrderBy(u => u.EstadoAcceso == EstadoAcceso.Pendiente ? 0 : 1)
            .ThenBy(u => u.NombreCompleto)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Usuario>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public Task<List<Usuario>> GetRadiologosDeHospitalAsync(Guid hospitalId, CancellationToken ct) =>
        Db.Usuarios
            .Where(u => u.Rol == RolUsuario.Radiologo
                        && u.EstadoAcceso == EstadoAcceso.Aprobado
                        && u.Hospitales.Any(h => h.HospitalId == hospitalId))
            .ToListAsync(ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Db.Usuarios.AnyAsync(ct);
}
