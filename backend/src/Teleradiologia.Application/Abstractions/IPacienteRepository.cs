using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IPacienteRepository
{
    Task<Paciente?> GetByDocumentoAsync(Guid hospitalId, string documentoIdentidad, CancellationToken ct);

    void Add(Paciente paciente);
}
