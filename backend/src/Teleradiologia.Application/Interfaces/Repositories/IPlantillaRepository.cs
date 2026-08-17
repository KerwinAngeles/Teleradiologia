using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Interfaces.Repositories;

public interface IPlantillaRepository
{
    // Siempre acotado al radiólogo: las plantillas son personales.
    Task<List<PlantillaInforme>> GetDelRadiologoAsync(Guid radiologoId, string? modalidad, CancellationToken ct);

    Task<PlantillaInforme?> GetByIdAsync(Guid radiologoId, Guid id, CancellationToken ct);

    Task<bool> ExisteNombreAsync(Guid radiologoId, string nombre, Guid? excepto, CancellationToken ct);

    void Add(PlantillaInforme plantilla);
}
