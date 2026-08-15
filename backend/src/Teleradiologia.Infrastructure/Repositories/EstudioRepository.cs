using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class EstudioRepository(AppDbContext db) : IEstudioRepository
{
    public Task<Estudio?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken ct) =>
        db.Estudios.Include(e => e.Paciente).FirstOrDefaultAsync(e => e.StudyInstanceUid == studyInstanceUid, ct);

    public Task<Estudio?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Estudios.Include(e => e.Paciente).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Estudio>> GetAllAsync(EstadoEstudio? estado, Guid? radiologoAsignadoId, CancellationToken ct)
    {
        var query = db.Estudios.Include(e => e.Paciente).AsQueryable();

        if (estado is not null)
        {
            query = query.Where(e => e.Estado == estado);
        }

        if (radiologoAsignadoId is not null)
        {
            query = query.Where(e => e.RadiologoAsignadoId == radiologoAsignadoId);
        }

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);
    }

    public void Add(Estudio estudio) => db.Estudios.Add(estudio);
}
