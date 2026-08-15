using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public interface IEstudioService
{
    Task<SubirEstudioResultado> SubirEstudioAsync(SubirEstudioRequest request, CancellationToken ct);

    Task<IReadOnlyList<EstudioResponse>> GetAllAsync(EstadoEstudio? estado, Guid? soloAsignadosAUsuario, CancellationToken ct);

    Task<EstudioResponse> TomarEstudioAsync(Guid estudioId, Guid radiologoId, CancellationToken ct);

    Task<EstudioResponse> ObtenerPorIdAsync(Guid estudioId, CancellationToken ct);

    Task<IReadOnlyList<ImagenEstudioResponse>> ObtenerImagenesAsync(Guid estudioId, Guid usuarioId, CancellationToken ct);

    Task<(byte[] Bytes, string ContentType)> ObtenerImagenAsync(Guid estudioId, string orthancInstanceId, CancellationToken ct);

    Task<byte[]> ObtenerArchivoDicomAsync(Guid estudioId, string orthancInstanceId, CancellationToken ct);
}
