namespace Teleradiologia.Infrastructure.Identity.Authentication;

public static class ClaimsLocales
{
    // Id del Usuario en NUESTRA base, no el `sub` del proveedor.
    public const string UsuarioId = "teleradiologia:usuario_id";

    public const string ProveedorUserId = "teleradiologia:proveedor_user_id";
}
