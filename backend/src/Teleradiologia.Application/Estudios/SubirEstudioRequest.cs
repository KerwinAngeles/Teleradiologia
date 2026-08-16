using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public record SubirEstudioRequest(
    IReadOnlyList<byte[]> ArchivosDicom,
    Guid HospitalId,
    PrioridadEstudio Prioridad,
    Guid SubidoPorId);
