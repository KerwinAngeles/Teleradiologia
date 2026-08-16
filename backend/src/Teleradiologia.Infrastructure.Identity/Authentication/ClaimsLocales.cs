namespace Teleradiologia.Infrastructure.Identity.Authentication;

public static class ClaimsLocales
{
    // Id del Usuario en NUESTRA base, no el `sub` del proveedor.
    public const string UsuarioId = "teleradiologia:usuario_id";

    public const string ProveedorUserId = "teleradiologia:proveedor_user_id";

    // Un claim por hospital habilitado. El Admin no lleva ninguno: ve todos.
    public const string HospitalId = "teleradiologia:hospital_id";
}
