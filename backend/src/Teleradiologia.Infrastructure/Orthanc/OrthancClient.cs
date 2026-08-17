using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common.Exceptions;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Infrastructure.Orthanc;

public class OrthancClient(HttpClient httpClient) : IOrthancClient
{
    public async Task<OrthancInstanciaSubida> SubirInstanciaAsync(byte[] archivoDicom, CancellationToken ct)
    {
        using var content = new ByteArrayContent(archivoDicom);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");

        var response = await httpClient.PostAsync("instances", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detalle = await response.Content.ReadAsStringAsync(ct);
            throw new ArchivoDicomInvalidoException(
                $"Orthanc rechazó el archivo ({(int)response.StatusCode}): {detalle}");
        }

        var payload = await response.Content.ReadFromJsonAsync<OrthancInstanceUploadResponse>(ct)
            ?? throw new ArchivoDicomInvalidoException("Respuesta vacía de Orthanc al subir la instancia.");

        return new OrthancInstanciaSubida(payload.ID, payload.ParentStudy, payload.Status == "AlreadyStored");
    }

    public async Task<OrthancEstudioMetadata> ObtenerMetadataInstanciaAsync(string orthancInstanceId, CancellationToken ct)
    {
        // Los DICOM reales traen sequences (arrays/objetos): se conservan solo los tags escalares.
        var tagsCrudos = await httpClient.GetFromJsonAsync<Dictionary<string, JsonElement>>(
            $"instances/{orthancInstanceId}/simplified-tags", ct)
            ?? throw new ArchivoDicomInvalidoException("No se pudieron leer los tags DICOM desde Orthanc.");

        var tags = tagsCrudos
            .Where(par => par.Value.ValueKind is JsonValueKind.String)
            .ToDictionary(par => par.Key, par => par.Value.GetString(), StringComparer.Ordinal);

        var studyInstanceUid = ObtenerTagRequerido(tags, "StudyInstanceUID");
        var pacienteDocumento = ObtenerTagRequerido(tags, "PatientID");

        return new OrthancEstudioMetadata(
            StudyInstanceUid: studyInstanceUid,
            DescripcionEstudio: tags.GetValueOrDefault("StudyDescription"),
            FechaEstudio: ParseFechaDicom(tags.GetValueOrDefault("StudyDate")),
            Modalidad: tags.GetValueOrDefault("Modality") is { Length: > 0 } modalidad ? modalidad : "OT",
            PacienteNombre: NormalizarNombreDicom(tags.GetValueOrDefault("PatientName")),
            PacienteDocumento: pacienteDocumento,
            PacienteFechaNacimiento: ParseFechaNacimientoDicom(tags.GetValueOrDefault("PatientBirthDate")),
            PacienteSexo: MapearSexoDicom(tags.GetValueOrDefault("PatientSex")));
    }

    public async Task<IReadOnlyList<OrthancInstanciaResumen>> ObtenerInstanciasDelEstudioAsync(string orthancStudyId, CancellationToken ct)
    {
        var instancias = await httpClient.GetFromJsonAsync<List<OrthancInstanceSummaryDto>>(
            $"studies/{orthancStudyId}/instances", ct) ?? [];

        // IndexInSeries lo calcula Orthanc: InstanceNumber puede faltar en DICOMs reales.
        return instancias
            .OrderBy(i => i.ParentSeries, StringComparer.Ordinal)
            .ThenBy(i => i.IndexInSeries)
            .Select(i => new OrthancInstanciaResumen(i.ID, i.ParentSeries, i.IndexInSeries, Cuadros(i)))
            .ToList();
    }

    public async Task<string?> ObtenerEstudioDeInstanciaAsync(string orthancInstanceId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"instances/{orthancInstanceId}/study", ct);
        if (!response.IsSuccessStatusCode)
        {
            // Una instancia inexistente no es un error del servicio: es un id que no vale.
            return null;
        }

        var estudio = await response.Content.ReadFromJsonAsync<OrthancStudyRefDto>(ct);
        return estudio?.ID;
    }

    private static int Cuadros(OrthancInstanceSummaryDto instancia) =>
        int.TryParse(instancia.MainDicomTags?.NumberOfFrames, out var cuadros) && cuadros > 0 ? cuadros : 1;

    public async Task<OrthancImagen> ObtenerImagenInstanciaAsync(string orthancInstanceId, CancellationToken ct)
    {
        // /rendered aplica la ventana del DICOM; /preview normaliza por min/max y lava la imagen.
        var response = await httpClient.GetAsync($"instances/{orthancInstanceId}/rendered", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ArchivoDicomInvalidoException($"No se pudo obtener la imagen de Orthanc ({(int)response.StatusCode}).");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
        return new OrthancImagen(bytes, contentType);
    }

    public async Task<byte[]> ObtenerArchivoDicomAsync(string orthancInstanceId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"instances/{orthancInstanceId}/file", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new ArchivoDicomInvalidoException(
                $"No se pudo obtener el DICOM de Orthanc ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static string ObtenerTagRequerido(Dictionary<string, string?> tags, string nombreTag) =>
        tags.GetValueOrDefault(nombreTag) is { Length: > 0 } valor
            ? valor
            : throw new ArchivoDicomInvalidoException($"El archivo DICOM no tiene el tag obligatorio '{nombreTag}'.");

    private static DateTimeOffset? ParseFechaDicom(string? valorDicom) =>
        DateTime.TryParseExact(valorDicom, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? new DateTimeOffset(fecha, TimeSpan.Zero)
            : null;

    private static DateOnly? ParseFechaNacimientoDicom(string? valorDicom) =>
        DateOnly.TryParseExact(valorDicom, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : null;

    private static string NormalizarNombreDicom(string? nombreDicom)
    {
        if (string.IsNullOrWhiteSpace(nombreDicom))
        {
            return "Desconocido";
        }

        var componentes = nombreDicom.Split('^', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return componentes.Length switch
        {
            0 => "Desconocido",
            1 => componentes[0],
            _ => string.Join(' ', componentes.Skip(1).Append(componentes[0])),
        };
    }

    private static SexoPaciente? MapearSexoDicom(string? valorDicom) => valorDicom?.Trim().ToUpperInvariant() switch
    {
        "M" => SexoPaciente.Masculino,
        "F" => SexoPaciente.Femenino,
        "O" => SexoPaciente.Otro,
        _ => null,
    };

    private record OrthancInstanceUploadResponse(string ID, string ParentStudy, string Status);

    private record OrthancStudyRefDto(string ID);

    // MainDicomTags trae NumberOfFrames como texto; ausente en las instancias de un solo cuadro.
    private record OrthancInstanceSummaryDto(
        string ID,
        string ParentSeries,
        int IndexInSeries,
        OrthancInstanceTagsDto? MainDicomTags);

    private record OrthancInstanceTagsDto(string? NumberOfFrames);
}
