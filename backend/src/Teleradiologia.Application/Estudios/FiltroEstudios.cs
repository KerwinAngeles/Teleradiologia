using Teleradiologia.Application.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public record FiltroEstudios : PageParams
{
    public EstadoEstudio? Estado { get; init; }

    public PrioridadEstudio? Prioridad { get; init; }

    public Guid? HospitalId { get; init; }

    public string? Modalidad { get; init; }

    // Nombre o documento del paciente.
    public string? Texto { get; init; }

    public bool AsignadoAMi { get; init; }

    // Abiertos con el plazo ya cumplido. Los estados finos del SLA (en plazo / por vencer)
    // se calculan al mapear y no se filtran en la base.
    public bool SoloVencidos { get; init; }

    public Guid? RadiologoAsignadoId { get; init; }
}
