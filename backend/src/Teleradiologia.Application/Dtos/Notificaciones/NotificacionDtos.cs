using Teleradiologia.Application.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Dtos.Notificaciones;

public record NotificacionDto(
    Guid Id,
    TipoNotificacion Tipo,
    string Titulo,
    string Mensaje,
    Guid? EstudioId,
    string? PacienteNombre,
    string? Modalidad,
    string? HospitalNombre,
    PrioridadEstudio? Prioridad,
    DateTimeOffset? LeidaAt,
    DateTimeOffset CreatedAt)
{
    public bool Leida => LeidaAt is not null;
}

public record FiltroNotificaciones : PageParams
{
    public TipoNotificacion? Tipo { get; init; }

    public bool? SoloNoLeidas { get; init; }

    // Busca en el título, el mensaje y el nombre del paciente del estudio.
    public string? Texto { get; init; }

    public DateTimeOffset? Desde { get; init; }

    public DateTimeOffset? Hasta { get; init; }
}

public record ResumenNotificaciones(int NoLeidas, IReadOnlyList<NotificacionDto> Recientes);
