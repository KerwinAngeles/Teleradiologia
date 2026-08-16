namespace Teleradiologia.Infrastructure.Security;

public class FirmaOptions
{
    public const string SectionName = "Firma";

    // Clave privada RSA en PEM (PKCS#8). En producción llega por variable de entorno.
    public string? ClavePrivadaPem { get; set; }

    // Solo desarrollo: si no hay clave configurada se genera una y se guarda acá, para que
    // las firmas sobrevivan a los reinicios. Regenerarla invalida todo lo firmado antes.
    public string RutaClaveDesarrollo { get; set; } = "keys/firma-dev.pem";
}
