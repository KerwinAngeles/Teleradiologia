using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Informes;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class InformeRepository(AppDbContext db) : IInformeRepository
{
    public Task<Informe?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Informes.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Informe?> GetConEstudioAsync(Guid id, CancellationToken ct) =>
        db.Informes
            .Include(i => i.Estudio).ThenInclude(e => e.Paciente)
            .Include(i => i.Estudio).ThenInclude(e => e.Hospital)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExisteParaEstudioAsync(Guid estudioId, CancellationToken ct) =>
        db.Informes.AnyAsync(i => i.EstudioId == estudioId, ct);

    public async Task<IReadOnlyList<Informe>> GetByEstudioAsync(Guid estudioId, CancellationToken ct) =>
        await db.Informes
            .Where(i => i.EstudioId == estudioId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<PagedResult<Informe>> BuscarAsync(FiltroInformes filtro, CancellationToken ct)
    {
        var query = db.Informes
            .Include(i => i.Estudio).ThenInclude(e => e.Paciente)
            .Include(i => i.Estudio).ThenInclude(e => e.Hospital)
            .AsQueryable();

        if (filtro.RadiologoId is { } radiologoId)
        {
            query = query.Where(i => i.RadiologoId == radiologoId);
        }

        if (filtro.SubidoPorId is { } subidoPorId)
        {
            query = query.Where(i => i.Estudio.SubidoPorId == subidoPorId);
        }

        if (filtro.Estado is { } estado)
        {
            query = query.Where(i => i.Estado == estado);
        }

        if (filtro.HospitalId is { } hospitalId)
        {
            query = query.Where(i => i.Estudio.HospitalId == hospitalId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Modalidad))
        {
            query = query.Where(i => i.Estudio.Modalidad == filtro.Modalidad);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Estudio.Paciente.NombreCompleto, texto) ||
                EF.Functions.ILike(i.Estudio.Paciente.DocumentoIdentidad, texto));
        }

        // La fecha del informe es la de la firma, y el alta mientras siga en borrador.
        if (filtro.Desde is { } desde)
        {
            query = query.Where(i => (i.FirmadoAt ?? i.CreatedAt) >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            query = query.Where(i => (i.FirmadoAt ?? i.CreatedAt) <= hasta);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.FirmadoAt ?? i.CreatedAt)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Informe>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public void Add(Informe informe) => db.Informes.Add(informe);
}
