using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class ResumenActividadRepository(AppDbContext db) : IResumenActividadRepository
{
    public Task<int> ContarEstudiosRecibidosAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct) =>
        db.Estudios.CountAsync(e => e.CreatedAt >= desde && e.CreatedAt < hasta, ct);

    public Task<int> ContarInformesFirmadosAsync(DateTimeOffset desde, DateTimeOffset hasta, bool adendas, CancellationToken ct) =>
        db.Informes.CountAsync(
            i => i.FirmadoAt != null
                && i.FirmadoAt >= desde
                && i.FirmadoAt < hasta
                && (adendas ? i.InformeAnteriorId != null : i.InformeAnteriorId == null),
            ct);

    public Task<int> ContarEstudiosPorEstadoAsync(EstadoEstudio estado, CancellationToken ct) =>
        db.Estudios.CountAsync(e => e.Estado == estado, ct);

    public async Task<List<FirmasPorRadiologo>> ContarFirmasPorRadiologoAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        // Tipo anónimo y no el record: EF no traduce el constructor posicional dentro del GroupBy.
        var filas = await db.Informes
            .Where(i => i.FirmadoAt != null && i.FirmadoAt >= desde && i.FirmadoAt < hasta)
            .GroupBy(i => i.RadiologoId)
            .Select(g => new { RadiologoId = g.Key, Firmados = g.Count() })
            .OrderByDescending(f => f.Firmados)
            .ToListAsync(ct);

        return [.. filas.Select(f => new FirmasPorRadiologo(f.RadiologoId, f.Firmados))];
    }
}
