using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Domain.Entities;

// Listado de establecimientos del MISPAS/SNS. Es referencia de solo lectura para dar de alta
// hospitales, no un inquilino de la plataforma.
public class EstablecimientoCatalogo
{
    public int Codigo { get; set; }

    [Required, MaxLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NivelAtencion { get; set; }

    [MaxLength(80)]
    public string? Tipo { get; set; }

    [MaxLength(50)]
    public string? RegionSalud { get; set; }

    [MaxLength(80)]
    public string? Provincia { get; set; }

    [MaxLength(120)]
    public string? Municipio { get; set; }

    public int? AnioApertura { get; set; }
}
