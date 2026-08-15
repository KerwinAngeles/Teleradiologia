using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class GenericRepository<TEntity>(AppDbContext db) : IGenericRepository<TEntity>
    where TEntity : class
{
    protected AppDbContext Db { get; } = db;

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await Db.Set<TEntity>().FindAsync([id], ct);

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken ct) =>
        await Db.Set<TEntity>().ToListAsync(ct);

    public virtual async Task<List<TEntity>> GetAllWithIncludeAsync(IReadOnlyList<string> propiedades, CancellationToken ct)
    {
        var query = Db.Set<TEntity>().AsQueryable();

        foreach (var propiedad in propiedades)
        {
            query = query.Include(propiedad);
        }

        return await query.ToListAsync(ct);
    }

    public virtual void Add(TEntity entity) => Db.Set<TEntity>().Add(entity);

    public virtual void Update(TEntity entity) => Db.Set<TEntity>().Update(entity);

    public virtual void Delete(TEntity entity) => Db.Set<TEntity>().Remove(entity);
}
