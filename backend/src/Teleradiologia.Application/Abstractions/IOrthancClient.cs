using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Abstractions;

public record OrthancInstanciaSubida(string OrthancInstanceId, string OrthancStudyId, bool YaExistia);

public record OrthancEstudioMetadata(
    string StudyInstanceUid,
    string? DescripcionEstudio,
    DateTimeOffset? FechaEstudio,
    string Modalidad,
    string PacienteNombre,
    string PacienteDocumento,
    DateOnly? PacienteFechaNacimiento,
    SexoPaciente? PacienteSexo);

public record OrthancInstanciaResumen(
    string OrthancInstanceId,
    string OrthancSeriesId,
    int NumeroInstancia,
    int NumeroDeCuadros);

public record OrthancImagen(byte[] Bytes, string ContentType);

public interface IOrthancClient
{
    Task<OrthancInstanciaSubida> SubirInstanciaAsync(byte[] archivoDicom, CancellationToken ct);

    Task<OrthancEstudioMetadata> ObtenerMetadataInstanciaAsync(string orthancInstanceId, CancellationToken ct);

    Task<IReadOnlyList<OrthancInstanciaResumen>> ObtenerInstanciasDelEstudioAsync(string orthancStudyId, CancellationToken ct);

    // Estudio de Orthanc del que cuelga la instancia, o null si la instancia no existe.
    // Es lo que permite comprobar que una imagen pertenece al estudio que se pidió.
    Task<string?> ObtenerEstudioDeInstanciaAsync(string orthancInstanceId, CancellationToken ct);

    Task<OrthancImagen> ObtenerImagenInstanciaAsync(string orthancInstanceId, CancellationToken ct);

    Task<byte[]> ObtenerArchivoDicomAsync(string orthancInstanceId, CancellationToken ct);
}
