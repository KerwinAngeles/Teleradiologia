using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public record EstudioResponse(
    Guid Id,
    string PacienteNombre,
    string PacienteDocumento,
    string Modalidad,
    string? DescripcionEstudio,
    string HospitalOrigen,
    DateTimeOffset FechaEstudio,
    EstadoEstudio Estado,
    Guid? RadiologoAsignadoId,
    string? RadiologoAsignadoNombre,
    Guid SubidoPorId,
    string SubidoPorNombre,
    DateTimeOffset CreatedAt);
