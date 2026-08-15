using Teleradiologia.Application.Common;

namespace Teleradiologia.Application.Interfaces.Auth;

// Cambiar de proveedor de identidad es escribir otra implementación de esta interfaz.
public interface IAuthProvider
{
    string Nombre { get; }

    Task<BaseResponse<ProveedorUsuario>> CrearUsuarioAsync(string email, string password, CancellationToken ct);

    Task<BaseResponse<ProveedorSesion>> IniciarSesionAsync(string email, string password, CancellationToken ct);

    Task<BaseResponse<ProveedorSesion>> RefrescarSesionAsync(string refreshToken, CancellationToken ct);

    Task<BaseResponse<bool>> EliminarUsuarioAsync(string proveedorUserId, CancellationToken ct);
}

public record ProveedorUsuario(string ProveedorUserId, string Email);

public record ProveedorSesion(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    string ProveedorUserId,
    string Email);
