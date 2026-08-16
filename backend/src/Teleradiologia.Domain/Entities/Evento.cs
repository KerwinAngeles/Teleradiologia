using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Evento
{
    public Guid Id { get; set; }

    [Required, MaxLength(80)]
    public string Entidad { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string EntidadId { get; set; } = string.Empty;

    public TipoOperacion Operacion { get; set; }

    public Guid? UsuarioId { get; set; }

    // Desnormalizado: la bitácora debe seguir siendo legible si el usuario se borra.
    [MaxLength(256)]
    public string? UsuarioEmail { get; set; }

    // JSON { campo: { antes, despues } }.
    public string? Cambios { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
