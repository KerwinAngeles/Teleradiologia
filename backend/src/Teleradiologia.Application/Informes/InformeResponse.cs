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
    DateTimeOffset? FirmadoAt,
    string? HashContenido,
    string? AlgoritmoFirma,
    string? FirmanteNombre,
    string? FirmanteMatricula,
    string? FirmaImagen);

public record VerificacionFirmaResponse(
    Guid InformeId,
    bool Valida,
    bool HashCoincide,
    bool FirmaValida,
    string? Motivo,
    string? HashGuardado,
    string HashCalculado,
    string? Algoritmo,
    string? FirmanteNombre,
    string? FirmanteMatricula,
    DateTimeOffset? FirmadoAt);
