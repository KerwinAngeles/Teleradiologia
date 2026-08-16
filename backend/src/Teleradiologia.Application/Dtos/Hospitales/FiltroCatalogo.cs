using Teleradiologia.Application.Common;

namespace Teleradiologia.Application.Dtos.Hospitales;

public record FiltroCatalogo : PageParams
{
    public string? Texto { get; init; }

    public string? Provincia { get; init; }

    // De los 1.910 establecimientos, la mayoría son centros de primer nivel sin equipos de
    // imagen. Este filtro deja acotar a los que pueden ser clientes reales.
    public string? Tipo { get; init; }
}
