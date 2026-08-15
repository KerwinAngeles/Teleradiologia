namespace Teleradiologia.Infrastructure.Identity.Options;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    // Firma HS256 (GoTrue self-hosted y proyectos cloud con clave simétrica).
    public string JwtSecret { get; set; } = string.Empty;

    // Alternativa asimétrica de Supabase Cloud. Si está seteada, gana sobre JwtSecret.
    public string? JwksUrl { get; set; }

    public string ServiceRoleKey { get; set; } = string.Empty;

    // Solo lo pide Supabase Cloud, que rutea por Kong.
    public string? AnonKey { get; set; }

    public string Audience { get; set; } = "authenticated";

    // GoTrue self-hosted no emite claim `iss`; dejar vacío desactiva la validación.
    public string? Issuer { get; set; }
}
