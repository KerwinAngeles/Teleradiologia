using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class PlantillaRepository(AppDbContext db) : IPlantillaRepository
{
    public Task<List<PlantillaInforme>> GetDelRadiologoAsync(Guid radiologoId, string? modalidad, CancellationToken ct)
    {
        var query = db.PlantillasInforme.Where(p => p.RadiologoId == radiologoId && p.Activa);

        if (!string.IsNullOrWhiteSpace(modalidad))
        {
            // Las de la modalidad del estudio y las genéricas; las de otra modalidad se ocultan.
            query = query.Where(p => p.Modalidad == null || p.Modalidad == modalidad);
        }

        return query
            .OrderByDescending(p => p.Favorita)
            .ThenByDescending(p => p.VecesUsada)
            .ThenBy(p => p.Nombre)
            .ToListAsync(ct);
    }

    public Task<PlantillaInforme?> GetByIdAsync(Guid radiologoId, Guid id, CancellationToken ct) =>
        db.PlantillasInforme.FirstOrDefaultAsync(p => p.Id == id && p.RadiologoId == radiologoId && p.Activa, ct);

    public Task<bool> ExisteNombreAsync(Guid radiologoId, string nombre, Guid? excepto, CancellationToken ct) =>
        db.PlantillasInforme.AnyAsync(
            p => p.RadiologoId == radiologoId
                 && p.Activa
                 && p.Nombre.ToLower() == nombre.ToLower()
                 && (excepto == null || p.Id != excepto),
            ct);

    public void Add(PlantillaInforme plantilla) => db.PlantillasInforme.Add(plantilla);
}
