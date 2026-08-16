using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Usuario : AuditableBaseEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Tecnico;

    public EstadoAcceso EstadoAcceso { get; set; } = EstadoAcceso.Pendiente;

    [Required, MaxLength(50)]
    public string Proveedor { get; set; } = string.Empty;

    // Claim `sub` del token del proveedor.
    [MaxLength(128)]
    public string? ProveedorUserId { get; set; }

    public DateTimeOffset? FechaDecision { get; set; }

    public Guid? DecididoPorId { get; set; }

    [MaxLength(500)]
    public string? MotivoDecision { get; set; }

    // Matrícula profesional del radiólogo: es lo que identifica al firmante en el informe.
    [MaxLength(50)]
    public string? Matricula { get; set; }

    public ICollection<UsuarioHospital> Hospitales { get; set; } = [];

    public bool PuedeIniciarSesion => EstadoAcceso == EstadoAcceso.Aprobado;
}
