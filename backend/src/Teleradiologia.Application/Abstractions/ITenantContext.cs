namespace Teleradiologia.Application.Abstractions;

public interface ITenantContext
{
    // true para el Admin, que no está acotado a hospitales.
    bool VeTodosLosHospitales { get; }

    IReadOnlyCollection<Guid> HospitalesPermitidos { get; }

    bool PuedeVer(Guid hospitalId);
}
