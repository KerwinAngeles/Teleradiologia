using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Application.Abstractions;

public interface IPacienteRepository
{
    Task<Paciente?> GetByDocumentoAsync(string documentoIdentidad, CancellationToken ct);

    void Add(Paciente paciente);
}
