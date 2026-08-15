using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Informes;

public record InformeResponse(
    Guid Id,
    Guid EstudioId,
    Guid RadiologoId,
    string RadiologoNombre,
    string Contenido,
    EstadoInforme Estado,
    bool EsAdenda,
    Guid? InformeAnteriorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirmadoAt);
