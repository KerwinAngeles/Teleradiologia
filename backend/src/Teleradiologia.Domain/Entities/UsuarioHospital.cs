namespace Teleradiologia.Domain.Entities;

// Habilitación de un usuario sobre un hospital. Un técnico suele tener uno; un radiólogo lee
// para varios. El Admin no lleva filas: no filtra por hospital.
public class UsuarioHospital
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public Guid HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? CreatedBy { get; set; }
}
