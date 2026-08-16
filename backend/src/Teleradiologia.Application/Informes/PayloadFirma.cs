using System.Globalization;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Informes;

public static class PayloadFirma
{
    // Versión actual del formato. Cada informe guarda con cuál se firmó, así un cambio de
    // formato no invalida las firmas anteriores.
    public const int VersionActual = 2;

    // Separador de unidad: no puede aparecer en el contenido, así nadie puede fabricar
    // un texto que produzca el mismo payload que otro.
    private const char Separador = '';

    // Postgres guarda timestamptz con precisión de microsegundos y DateTimeOffset tiene ticks
    // de 100 ns. Firmar con los 7 decimales hacía que el hash no coincidiera al releer de la
    // base: el séptimo dígito se perdía al guardar.
    private const string FormatoInstante = "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'";

    // El orden y el formato tienen que ser idénticos al firmar y al verificar.
    public static string Construir(Informe informe, Estudio estudio, int version)
    {
        var campos = new List<string>
        {
            informe.Id.ToString(),
            informe.EstudioId.ToString(),
            estudio.StudyInstanceUid,
            estudio.Paciente?.DocumentoIdentidad ?? string.Empty,
            informe.RadiologoId.ToString(),
            informe.FirmanteMatricula ?? string.Empty,
            informe.FirmadoAt?.ToUniversalTime().ToString(FormatoInstante, CultureInfo.InvariantCulture) ?? string.Empty,
            informe.InformeAnteriorId?.ToString() ?? string.Empty,
            informe.Contenido,
        };

        // v2 suma el trazo: sin esto se podría cambiar la firma manuscrita sin romper el hash.
        if (version >= 2)
        {
            campos.Add(informe.FirmaImagen ?? string.Empty);
        }

        return string.Join(Separador, campos);
    }

    public static string Resumen(string hash) => hash.Length <= 16 ? hash : $"{hash[..8]}…{hash[^8..]}";
}
