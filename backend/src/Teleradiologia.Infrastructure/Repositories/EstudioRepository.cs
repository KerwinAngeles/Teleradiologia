using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Estudios;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class EstudioRepository(AppDbContext db) : IEstudioRepository
{
    public Task<Estudio?> GetExistenteAsync(Guid hospitalId, string orthancStudyId, string studyInstanceUid, CancellationToken ct) =>
        db.Estudios.Include(e => e.Paciente).Include(e => e.Hospital)
            .FirstOrDefaultAsync(
                e => e.HospitalId == hospitalId &&
                     (e.OrthancStudyId == orthancStudyId || e.StudyInstanceUid == studyInstanceUid),
                ct);

    public Task<Estudio?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Estudios.Include(e => e.Paciente).Include(e => e.Hospital)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<PagedResult<Estudio>> BuscarAsync(FiltroEstudios filtro, CancellationToken ct)
    {
        var query = db.Estudios.Include(e => e.Paciente).Include(e => e.Hospital).AsQueryable();

        if (filtro.Estado is { } estado)
        {
            query = query.Where(e => e.Estado == estado);
        }

        if (filtro.Prioridad is { } prioridad)
        {
            query = query.Where(e => e.Prioridad == prioridad);
        }

        if (filtro.HospitalId is { } hospitalId)
        {
            query = query.Where(e => e.HospitalId == hospitalId);
        }

        if (filtro.RadiologoAsignadoId is { } radiologoId)
        {
            query = query.Where(e => e.RadiologoAsignadoId == radiologoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Modalidad))
        {
            query = query.Where(e => e.Modalidad == filtro.Modalidad);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.Paciente.NombreCompleto, texto) ||
                EF.Functions.ILike(e.Paciente.DocumentoIdentidad, texto) ||
                EF.Functions.ILike(e.OrthancStudyId, texto));
        }

        if (filtro.SoloVencidos)
        {
            var ahora = DateTimeOffset.UtcNow;
            query = query.Where(e => e.InformadoAt == null && e.FechaLimite <= ahora);
        }

        var total = await query.CountAsync(ct);

        // Los cerrados no compiten por atención: van al final, por vencimiento.
        var items = await query
            .OrderBy(e => e.Estado == EstadoEstudio.Informado)
            .ThenBy(e => e.FechaLimite)
            .ThenBy(e => e.CreatedAt)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Estudio>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public Task<List<EstudioEstadisticaDto>> ProyectarEstadisticasAsync(CancellationToken ct)
    {
        var ahora = DateTimeOffset.UtcNow;

        return db.Estudios
            .Select(e => new EstudioEstadisticaDto(
                e.Estado,
                e.Prioridad,
                e.Modalidad,
                e.Hospital.Nombre,
                e.SubidoPorId,
                e.RadiologoAsignadoId,
                e.InformadoAt == null && e.FechaLimite <= ahora))
            .ToListAsync(ct);
    }

    public void Add(Estudio estudio) => db.Estudios.Add(estudio);
}
