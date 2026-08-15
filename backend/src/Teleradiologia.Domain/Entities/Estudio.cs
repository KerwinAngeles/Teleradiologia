using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Estudio : AuditableBaseEntity
{
    public Guid Id { get; set; }

    public Guid PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    [Required, MaxLength(64)]
    public string OrthancStudyId { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string StudyInstanceUid { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string Modalidad { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DescripcionEstudio { get; set; }

    [Required, MaxLength(200)]
    public string HospitalOrigen { get; set; } = string.Empty;

    public DateTimeOffset FechaEstudio { get; set; }

    public EstadoEstudio Estado { get; set; } = EstadoEstudio.Pendiente;

    public Guid? RadiologoAsignadoId { get; set; }

    public Guid SubidoPorId { get; set; }

    public ICollection<Informe> Informes { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
