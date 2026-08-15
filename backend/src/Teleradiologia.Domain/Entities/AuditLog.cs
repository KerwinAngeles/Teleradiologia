using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid? EstudioId { get; set; }
    public Estudio? Estudio { get; set; }

    public TipoAccionAuditoria Accion { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(45)]
    public string? DireccionIp { get; set; }
}
