using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public record EstudioResponse(
    Guid Id,
    string PacienteNombre,
    string PacienteDocumento,
    // La hoja imprimible los muestra en la grilla de identificación del paciente.
    SexoPaciente PacienteSexo,
    DateOnly PacienteFechaNacimiento,
    string Modalidad,
    string? DescripcionEstudio,
    Guid HospitalId,
    string HospitalNombre,
    string? HospitalProvincia,
    string? HospitalMunicipio,
    string StudyInstanceUid,
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
