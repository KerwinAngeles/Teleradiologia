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

    public Guid HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public DateTimeOffset FechaEstudio { get; set; }

    public EstadoEstudio Estado { get; set; } = EstadoEstudio.Pendiente;

    public PrioridadEstudio Prioridad { get; set; } = PrioridadEstudio.Rutina;

    // Se fija al recibir el estudio y no se recalcula: un cambio de contrato no mueve
    // el plazo de lo que ya entró.
    public DateTimeOffset FechaLimite { get; set; }

    public DateTimeOffset? AsignadoAt { get; set; }

    public DateTimeOffset? InformadoAt { get; set; }

    public Guid? RadiologoAsignadoId { get; set; }

    public Guid SubidoPorId { get; set; }

    public ICollection<Informe> Informes { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
