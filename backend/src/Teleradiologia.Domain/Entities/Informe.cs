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

    [MaxLength(64)]
    public string? HashContenido { get; set; }

    public string? Firma { get; set; }

    [MaxLength(30)]
    public string? AlgoritmoFirma { get; set; }

    // Copiados al firmar: el documento no puede cambiar si la persona después corrige sus datos.
    [MaxLength(200)]
    public string? FirmanteNombre { get; set; }

    [MaxLength(50)]
    public string? FirmanteMatricula { get; set; }

    // PNG en data URL. Entra en el payload firmado: cambiarla invalida la firma.
    public string? FirmaImagen { get; set; }

    public int? VersionFirma { get; set; }

    public bool EstaFirmado => Estado == EstadoInforme.Firmado && Firma is not null;
}
