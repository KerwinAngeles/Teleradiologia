using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teleradiologia.Application.Abstractions;

namespace Teleradiologia.Infrastructure.Security;

public class FirmaDigitalService : IFirmaDigitalService, IDisposable
{
    private readonly RSA _rsa;

    public FirmaDigitalService(
        IOptions<FirmaOptions> options,
        IHostEnvironment entorno,
        ILogger<FirmaDigitalService> logger)
    {
        _rsa = RSA.Create(2048);

        var config = options.Value;

        if (!string.IsNullOrWhiteSpace(config.ClavePrivadaPem))
        {
            _rsa.ImportFromPem(config.ClavePrivadaPem);
            return;
        }

        if (!entorno.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Firma:ClavePrivadaPem no está configurada. Sin clave no se pueden firmar informes.");
        }

        CargarOCrearClaveDeDesarrollo(config.RutaClaveDesarrollo, entorno.ContentRootPath, logger);
    }

    public string Algoritmo => "RS256";

    public FirmaGenerada Firmar(string payload)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var firma = _rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return new FirmaGenerada(Convert.ToHexString(hash).ToLowerInvariant(), Convert.ToBase64String(firma), Algoritmo);
    }

    public ResultadoVerificacion Verificar(string payload, string? hashGuardado, string? firmaGuardada)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var hashCalculado = Convert.ToHexString(hash).ToLowerInvariant();

        // El hash se compara contra el guardado: si el contenido cambió después de firmar,
        // deja de coincidir aunque la firma en sí siga siendo válida sobre el texto original.
        var hashCoincide = string.Equals(hashCalculado, hashGuardado, StringComparison.OrdinalIgnoreCase);

        var firmaValida = false;
        if (!string.IsNullOrWhiteSpace(firmaGuardada))
        {
            try
            {
                firmaValida = _rsa.VerifyHash(
                    hash, Convert.FromBase64String(firmaGuardada), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (FormatException)
            {
                // Firma corrupta o manipulada: no es válida.
            }
        }

        return new ResultadoVerificacion(hashCoincide, firmaValida, hashCalculado);
    }

    private void CargarOCrearClaveDeDesarrollo(string rutaRelativa, string raiz, ILogger logger)
    {
        var ruta = Path.IsPathRooted(rutaRelativa) ? rutaRelativa : Path.Combine(raiz, rutaRelativa);

        if (File.Exists(ruta))
        {
            _rsa.ImportFromPem(File.ReadAllText(ruta));
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        File.WriteAllText(ruta, _rsa.ExportPkcs8PrivateKeyPem());

        logger.LogWarning(
            "No había clave de firma: se generó una de desarrollo en {Ruta}. Si la borrás, " +
            "los informes ya firmados dejan de poder verificarse.", ruta);
    }

    public void Dispose() => _rsa.Dispose();
}
