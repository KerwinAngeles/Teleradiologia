using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Informes;

// Fila del listado: trae del estudio lo justo para identificarlo sin abrirlo, y no
// el contenido, que en un listado de 20 filas serían varios cientos de KB al pedo.
public record InformeListadoResponse(
    Guid Id,
    Guid EstudioId,
    string PacienteNombre,
    string PacienteDocumento,
    string Modalidad,
    string HospitalNombre,
    DateTimeOffset FechaEstudio,
    EstadoInforme Estado,
    bool EsAdenda,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirmadoAt,
    string RadiologoNombre);

// La hoja imprimible necesita además el contenido y los datos de la firma.
public record InformeDetalleResponse(
    Guid Id,
    Guid EstudioId,
    string PacienteNombre,
    string PacienteDocumento,
    string Modalidad,
    string? DescripcionEstudio,
    string HospitalNombre,
    DateTimeOffset FechaEstudio,
    string Contenido,
    EstadoInforme Estado,
    bool EsAdenda,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirmadoAt,
    string RadiologoNombre,
    string? HashContenido,
    string? AlgoritmoFirma,
    string? FirmanteNombre,
    string? FirmanteMatricula,
    string? FirmaImagen);
