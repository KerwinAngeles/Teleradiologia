using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Notificaciones;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class NotificacionRepository(AppDbContext db) : INotificacionRepository
{
    public async Task<PagedResult<Notificacion>> BuscarAsync(Guid usuarioId, FiltroNotificaciones filtro, CancellationToken ct)
    {
        var query = Base(usuarioId);

        if (filtro.Tipo is { } tipo)
        {
            query = query.Where(n => n.Tipo == tipo);
        }

        if (filtro.SoloNoLeidas == true)
        {
            query = query.Where(n => n.LeidaAt == null);
        }

        if (filtro.Desde is { } desde)
        {
            query = query.Where(n => n.CreatedAt >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            query = query.Where(n => n.CreatedAt < hasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Titulo, texto) ||
                EF.Functions.ILike(n.Mensaje, texto) ||
                (n.Estudio != null && EF.Functions.ILike(n.Estudio.Paciente.NombreCompleto, texto)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Notificacion>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public Task<List<Notificacion>> GetRecientesAsync(Guid usuarioId, int cantidad, CancellationToken ct) =>
        Base(usuarioId).OrderByDescending(n => n.CreatedAt).Take(cantidad).ToListAsync(ct);

    public Task<int> ContarNoLeidasAsync(Guid usuarioId, CancellationToken ct) =>
        db.Notificaciones.CountAsync(n => n.UsuarioId == usuarioId && n.LeidaAt == null, ct);

    public Task<Notificacion?> GetByIdAsync(Guid usuarioId, Guid id, CancellationToken ct) =>
        db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct);

    public Task<int> MarcarTodasLeidasAsync(Guid usuarioId, CancellationToken ct) =>
        db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && n.LeidaAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.LeidaAt, DateTimeOffset.UtcNow), ct);

    public void AddRange(IEnumerable<Notificacion> notificaciones) => db.Notificaciones.AddRange(notificaciones);

    // IgnoreQueryFilters en el Estudio incluido: la notificación ya está acotada a su
    // destinatario, y el filtro de hospital escondería el estudio si al radiólogo le
    // revocaron ese hospital después de avisarle.
    private IQueryable<Notificacion> Base(Guid usuarioId) =>
        db.Notificaciones
            .IgnoreQueryFilters()
            .Include(n => n.Estudio)!.ThenInclude(e => e.Paciente)
            .Include(n => n.Estudio)!.ThenInclude(e => e.Hospital)
            .Where(n => n.UsuarioId == usuarioId);
}
