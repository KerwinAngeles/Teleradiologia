using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public record EstudioResponse(
    Guid Id,
    string PacienteNombre,
    string PacienteDocumento,
    string Modalidad,
    string? DescripcionEstudio,
    Guid HospitalId,
    string HospitalNombre,
    DateTimeOffset FechaEstudio,
    EstadoEstudio Estado,
    PrioridadEstudio Prioridad,
    DateTimeOffset FechaLimite,
    EstadoSla EstadoSla,
    int MinutosRestantes,
    DateTimeOffset? AsignadoAt,
    DateTimeOffset? InformadoAt,
    Guid? RadiologoAsignadoId,
    string? RadiologoAsignadoNombre,
    Guid SubidoPorId,
    string SubidoPorNombre,
    DateTimeOffset CreatedAt);
