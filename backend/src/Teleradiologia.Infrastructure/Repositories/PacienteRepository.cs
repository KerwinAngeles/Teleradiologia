using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class PacienteRepository(AppDbContext db) : IPacienteRepository
{
    public Task<Paciente?> GetByDocumentoAsync(Guid hospitalId, string documentoIdentidad, CancellationToken ct) =>
        db.Pacientes.FirstOrDefaultAsync(
            p => p.HospitalId == hospitalId && p.DocumentoIdentidad == documentoIdentidad, ct);

    public void Add(Paciente paciente) => db.Pacientes.Add(paciente);
}
