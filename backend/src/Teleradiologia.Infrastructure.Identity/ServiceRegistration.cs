using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Interfaces.Auth;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Infrastructure.Identity.Authentication;
using Teleradiologia.Infrastructure.Identity.Options;
using Teleradiologia.Infrastructure.Identity.Providers;
using Teleradiologia.Infrastructure.Identity.Services;

namespace Teleradiologia.Infrastructure.Identity;

public static class ServiceRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var opciones = configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()
            ?? throw new InvalidOperationException("Falta la sección 'Supabase' en la configuración.");

        Validar(opciones);

        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));

        services.AddHttpClient<IAuthProvider, SupabaseAuthProvider>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;

            client.BaseAddress = new Uri(config.Url.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ServiceRoleKey);

            // Supabase Cloud rutea por Kong, que exige apikey. GoTrue self-hosted la ignora.
            if (!string.IsNullOrWhiteSpace(config.AnonKey))
            {
                client.DefaultRequestHeaders.Add("apikey", config.AnonKey);
            }
        });

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IClaimsTransformation, UsuarioClaimsTransformation>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigurarJwt(options, opciones));

        services.AddAuthorization(options =>
        {
            // Exige el claim que agrega UsuarioClaimsTransformation: una cuenta no aprobada da 403.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(ClaimsLocales.UsuarioId)
                .Build();

            options.DefaultPolicy = options.FallbackPolicy;
        });

        return services;
    }

    private static void ConfigurarJwt(JwtBearerOptions options, SupabaseOptions supabase)
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = supabase.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "email",
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,

            // GoTrue self-hosted no emite `iss`; Supabase Cloud sí.
            ValidateIssuer = !string.IsNullOrWhiteSpace(supabase.Issuer),
            ValidIssuer = supabase.Issuer,
        };

        if (!string.IsNullOrWhiteSpace(supabase.JwksUrl))
        {
            options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                supabase.JwksUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = supabase.JwksUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) });
        }
        else
        {
            options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            options.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabase.JwtSecret));
        }
    }

    private static void Validar(SupabaseOptions opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.Url))
        {
            throw new InvalidOperationException("Supabase:Url no está configurada.");
        }

        if (string.IsNullOrWhiteSpace(opciones.ServiceRoleKey))
        {
            throw new InvalidOperationException(
                "Supabase:ServiceRoleKey no está configurada — sin ella no se pueden dar de alta usuarios.");
        }

        if (string.IsNullOrWhiteSpace(opciones.JwksUrl) && Encoding.UTF8.GetByteCount(opciones.JwtSecret) < 32)
        {
            throw new InvalidOperationException(
                "Supabase:JwtSecret no está configurada o mide menos de 32 bytes. Configurala vía " +
                "variables de entorno — nunca hardcodeada fuera de desarrollo local.");
        }
    }
}
