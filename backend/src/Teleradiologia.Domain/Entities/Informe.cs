using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Informe : AuditableBaseEntity
{
    public Guid Id { get; set; }

    public Guid EstudioId { get; set; }
    public Estudio Estudio { get; set; } = null!;

    public Guid RadiologoId { get; set; }

    [Required]
    public string Contenido { get; set; } = string.Empty;

    public EstadoInforme Estado { get; set; } = EstadoInforme.Borrador;

    public Guid? InformeAnteriorId { get; set; }
    public Informe? InformeAnterior { get; set; }

    public DateTimeOffset? FirmadoAt { get; set; }
}
