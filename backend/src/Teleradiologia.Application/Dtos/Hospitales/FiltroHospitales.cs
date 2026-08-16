using Teleradiologia.Application.Common;

namespace Teleradiologia.Application.Dtos.Hospitales;

public record FiltroHospitales : PageParams
{
    public string? Texto { get; init; }

    public string? Provincia { get; init; }

    public bool? Activo { get; init; }
}
