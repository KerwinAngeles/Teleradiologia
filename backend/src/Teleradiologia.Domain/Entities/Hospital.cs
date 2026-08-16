using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;

namespace Teleradiologia.Domain.Entities;

public class Hospital : AuditableBaseEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    // ID_CENTRO del catálogo del MISPAS. Null en los privados, que no figuran en el listado público.
    public int? CodigoExterno { get; set; }
    public EstablecimientoCatalogo? Catalogo { get; set; }

    [MaxLength(80)]
    public string? Provincia { get; set; }

    [MaxLength(120)]
    public string? Municipio { get; set; }

    [MaxLength(256)]
    public string? EmailContacto { get; set; }

    public bool Activo { get; set; } = true;

    // Plazo contratado por prioridad. Null usa el valor global de configuración.
    public int? SlaStatMinutos { get; set; }
    public int? SlaUrgenteMinutos { get; set; }
    public int? SlaRutinaMinutos { get; set; }

    public ICollection<UsuarioHospital> Usuarios { get; set; } = [];
}
