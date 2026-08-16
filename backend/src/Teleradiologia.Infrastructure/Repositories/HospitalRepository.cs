using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Hospitales;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class HospitalRepository(AppDbContext db) : GenericRepository<Hospital>(db), IHospitalRepository
{
    public Task<List<Hospital>> GetActivosAsync(CancellationToken ct) =>
        Db.Hospitales.Where(h => h.Activo).OrderBy(h => h.Nombre).ToListAsync(ct);

    // IgnoreQueryFilters: al dar de alta un hospital, el Admin todavía no lo tiene habilitado.
    public Task<bool> ExisteNombreAsync(string nombre, CancellationToken ct) =>
        Db.Hospitales.IgnoreQueryFilters().AnyAsync(h => h.Nombre.ToLower() == nombre.ToLower(), ct);

    public async Task<PagedResult<Hospital>> BuscarAsync(FiltroHospitales filtro, CancellationToken ct)
    {
        var query = Db.Hospitales.AsQueryable();

        if (filtro.Activo is { } activo)
        {
            query = query.Where(h => h.Activo == activo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Provincia))
        {
            query = query.Where(h => h.Provincia == filtro.Provincia);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(h => EF.Functions.ILike(h.Nombre, texto));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(h => h.Nombre)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Hospital>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public Task<List<string>> GetProvinciasAsync(CancellationToken ct) =>
        Db.CatalogoEstablecimientos
            .Where(e => e.Provincia != null)
            .Select(e => e.Provincia!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);

    public Task<List<string>> GetTiposCatalogoAsync(CancellationToken ct) =>
        Db.CatalogoEstablecimientos
            .Where(e => e.Tipo != null)
            .Select(e => e.Tipo!)
            .Distinct()
            .OrderBy(tipo => tipo)
            .ToListAsync(ct);

    public async Task<PagedResult<EstablecimientoCatalogo>> BuscarEnCatalogoAsync(FiltroCatalogo filtro, CancellationToken ct)
    {
        var query = Db.CatalogoEstablecimientos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(e => EF.Functions.ILike(e.Nombre, texto));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Provincia))
        {
            query = query.Where(e => e.Provincia == filtro.Provincia);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Tipo))
        {
            query = query.Where(e => e.Tipo == filtro.Tipo);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.Nombre)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<EstablecimientoCatalogo>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }
}
