using Teleradiologia.Application.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Dtos.Eventos;

public record EventoDto(
    Guid Id,
    string Entidad,
    string EntidadId,
    TipoOperacion Operacion,
    Guid? UsuarioId,
    string? UsuarioEmail,
    string? Cambios,
    DateTimeOffset Timestamp);

public record FiltroEventos : PageParams
{
    public string? Entidad { get; init; }

    public TipoOperacion? Operacion { get; init; }

    public Guid? UsuarioId { get; init; }

    // Busca en el email del autor y en el id de la entidad afectada.
    public string? Texto { get; init; }

    public DateTimeOffset? Desde { get; init; }

    public DateTimeOffset? Hasta { get; init; }
}

public record KpisEventosDto(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    int Total,
    int Creaciones,
    int Modificaciones,
    int Eliminaciones,
    int UsuariosActivos,
    IReadOnlyList<ConteoPorClaveDto> PorEntidad,
    IReadOnlyList<ConteoPorClaveDto> PorUsuario);

public record ConteoPorClaveDto(string Clave, int Cantidad);
