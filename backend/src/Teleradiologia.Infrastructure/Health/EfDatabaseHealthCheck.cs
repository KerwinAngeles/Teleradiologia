using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Health;

public class EfDatabaseHealthCheck(AppDbContext db) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}
