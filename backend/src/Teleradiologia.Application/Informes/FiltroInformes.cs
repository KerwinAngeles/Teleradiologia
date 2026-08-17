using Teleradiologia.Application.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Informes;

public record FiltroInformes : PageParams
{
    public EstadoInforme? Estado { get; init; }

    public string? Modalidad { get; init; }

    public Guid? HospitalId { get; init; }

    // Nombre o documento del paciente.
    public string? Texto { get; init; }

    // Sobre la fecha del informe: la firma si está firmado, el alta si es borrador.
    public DateTimeOffset? Desde { get; init; }

    public DateTimeOffset? Hasta { get; init; }

    // Alcance por rol. El controlador SIEMPRE los reescribe: si llegaran del cliente,
    // un radiólogo podría pedir los informes de otro con solo cambiar la query string.
    public Guid? RadiologoId { get; init; }

    public Guid? SubidoPorId { get; init; }
}
