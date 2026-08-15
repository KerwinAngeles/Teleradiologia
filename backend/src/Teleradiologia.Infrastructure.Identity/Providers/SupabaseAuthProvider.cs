using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Interfaces.Auth;

namespace Teleradiologia.Infrastructure.Identity.Providers;

public class SupabaseAuthProvider(HttpClient http, ILogger<SupabaseAuthProvider> logger) : IAuthProvider
{
    public string Nombre => "supabase";

    public async Task<BaseResponse<ProveedorUsuario>> CrearUsuarioAsync(string email, string password, CancellationToken ct)
    {
        var payload = new { email, password, email_confirm = true };

        using var respuesta = await http.PostAsJsonAsync("admin/users", payload, ct);

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await LeerErrorAsync(respuesta, ct);

            if (respuesta.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Conflict ||
                error.Contains("already been registered", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return BaseResponse<ProveedorUsuario>.Fail("Ese email ya está registrado.", ErrorCode.Conflicto);
            }

            logger.LogError("Supabase rechazó el alta de {Email}: {Status} {Error}", email, respuesta.StatusCode, error);
            return BaseResponse<ProveedorUsuario>.Fail($"El proveedor de identidad rechazó el alta: {error}", ErrorCode.ServicioExterno);
        }

        var usuario = await respuesta.Content.ReadFromJsonAsync<RespuestaUsuario>(cancellationToken: ct);

        return usuario is null or { Id: null }
            ? BaseResponse<ProveedorUsuario>.Fail("El proveedor de identidad devolvió una respuesta vacía.", ErrorCode.ServicioExterno)
            : BaseResponse<ProveedorUsuario>.Success(new ProveedorUsuario(usuario.Id, usuario.Email ?? email));
    }

    public Task<BaseResponse<ProveedorSesion>> IniciarSesionAsync(string email, string password, CancellationToken ct) =>
        PedirTokenAsync("token?grant_type=password", new { email, password }, esLogin: true, ct);

    public Task<BaseResponse<ProveedorSesion>> RefrescarSesionAsync(string refreshToken, CancellationToken ct) =>
        PedirTokenAsync("token?grant_type=refresh_token", new { refresh_token = refreshToken }, esLogin: false, ct);

    public async Task<BaseResponse<bool>> EliminarUsuarioAsync(string proveedorUserId, CancellationToken ct)
    {
        using var respuesta = await http.DeleteAsync($"admin/users/{proveedorUserId}", ct);

        if (respuesta.IsSuccessStatusCode || respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return BaseResponse<bool>.Success(true);
        }

        var error = await LeerErrorAsync(respuesta, ct);
        logger.LogError("No se pudo borrar {UserId} en Supabase: {Status} {Error}", proveedorUserId, respuesta.StatusCode, error);

        return BaseResponse<bool>.Fail($"No se pudo eliminar la credencial: {error}", ErrorCode.ServicioExterno);
    }

    private async Task<BaseResponse<ProveedorSesion>> PedirTokenAsync(string ruta, object payload, bool esLogin, CancellationToken ct)
    {
        using var respuesta = await http.PostAsJsonAsync(ruta, payload, ct);

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await LeerErrorAsync(respuesta, ct);

            if (respuesta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                return BaseResponse<ProveedorSesion>.Fail(
                    esLogin ? "Email o contraseña incorrectos." : "La sesión expiró. Volvé a iniciar sesión.",
                    ErrorCode.NoAutenticado);
            }

            logger.LogError("Supabase falló al emitir token: {Status} {Error}", respuesta.StatusCode, error);
            return BaseResponse<ProveedorSesion>.Fail($"El proveedor de identidad no respondió: {error}", ErrorCode.ServicioExterno);
        }

        var token = await respuesta.Content.ReadFromJsonAsync<RespuestaToken>(cancellationToken: ct);

        if (token is null or { AccessToken: null } || token.User?.Id is null)
        {
            return BaseResponse<ProveedorSesion>.Fail("El proveedor de identidad devolvió una sesión incompleta.", ErrorCode.ServicioExterno);
        }

        return BaseResponse<ProveedorSesion>.Success(new ProveedorSesion(
            token.AccessToken,
            token.RefreshToken,
            DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt),
            token.User.Id,
            token.User.Email ?? string.Empty));
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return respuesta.ReasonPhrase ?? respuesta.StatusCode.ToString();
        }

        // GoTrue no usa un único formato de error: según el endpoint devuelve msg, message o error_description.
        try
        {
            var json = JsonDocument.Parse(cuerpo).RootElement;

            foreach (var campo in (string[])["msg", "message", "error_description", "error"])
            {
                if (json.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String)
                {
                    return valor.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // Cuerpo no-JSON: se devuelve crudo.
        }

        return cuerpo.Length > 300 ? cuerpo[..300] : cuerpo;
    }

    private sealed record RespuestaUsuario(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("email")] string? Email);

    private sealed record RespuestaToken(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_at")] long ExpiresAt,
        [property: JsonPropertyName("user")] RespuestaUsuario? User);
}
