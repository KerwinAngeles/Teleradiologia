using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Notificacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public TipoNotificacion Tipo { get; set; }

    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Mensaje { get; set; } = string.Empty;

    public Guid? EstudioId { get; set; }
    public Estudio? Estudio { get; set; }

    public Guid? HospitalId { get; set; }

    public DateTimeOffset? LeidaAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool Leida => LeidaAt is not null;
}
