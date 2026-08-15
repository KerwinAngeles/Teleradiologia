using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class InformeRepository(AppDbContext db) : IInformeRepository
{
    public Task<Informe?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Informes.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExisteParaEstudioAsync(Guid estudioId, CancellationToken ct) =>
        db.Informes.AnyAsync(i => i.EstudioId == estudioId, ct);

    public async Task<IReadOnlyList<Informe>> GetByEstudioAsync(Guid estudioId, CancellationToken ct) =>
        await db.Informes
            .Where(i => i.EstudioId == estudioId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public void Add(Informe informe) => db.Informes.Add(informe);
}
