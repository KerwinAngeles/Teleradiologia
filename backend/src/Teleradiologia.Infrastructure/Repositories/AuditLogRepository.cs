using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public void Add(AuditLog auditLog) => db.AuditLogs.Add(auditLog);
}
