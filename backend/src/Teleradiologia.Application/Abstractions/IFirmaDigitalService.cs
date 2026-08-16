namespace Teleradiologia.Application.Abstractions;

// Frontera con el mecanismo de firma. Hoy es una clave RSA propia; el día que se use un
// certificado cualificado o un HSM se cambia la implementación, no los casos de uso.
public interface IFirmaDigitalService
{
    string Algoritmo { get; }

    FirmaGenerada Firmar(string payload);

    ResultadoVerificacion Verificar(string payload, string? hashGuardado, string? firmaGuardada);
}

public record FirmaGenerada(string Hash, string Firma, string Algoritmo);

public record ResultadoVerificacion(bool HashCoincide, bool FirmaValida, string HashCalculado)
{
    public bool EsValida => HashCoincide && FirmaValida;
}
