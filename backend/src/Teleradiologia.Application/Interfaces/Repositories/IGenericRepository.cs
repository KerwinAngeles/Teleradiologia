namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IGenericRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<List<TEntity>> GetAllAsync(CancellationToken ct);

    Task<List<TEntity>> GetAllWithIncludeAsync(IReadOnlyList<string> propiedades, CancellationToken ct);

    // No confirman: la transacción la cierra IUnitOfWork.SaveChangesAsync.
    void Add(TEntity entity);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}
