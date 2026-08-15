using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);
}
