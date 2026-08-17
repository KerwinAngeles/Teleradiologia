using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;

namespace Teleradiologia.Domain.Entities;

public class PlantillaInforme : AuditableBaseEntity
{
    public Guid Id { get; set; }

    public Guid RadiologoId { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    // Null = sirve para cualquier modalidad. Si tiene valor, se ofrece primero en estudios de esa.
    [MaxLength(16)]
    public string? Modalidad { get; set; }

    [MaxLength(120)]
    public string? RegionAnatomica { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    // JSON: [{ "titulo": "...", "contenido": "...", "orden": 0 }]
    public string Secciones { get; set; } = "[]";

    public bool Favorita { get; set; }

    public int VecesUsada { get; set; }

    // Baja lógica: un informe firmado puede haber salido de una plantilla que ya no se usa.
    public bool Activa { get; set; } = true;
}
