using Teleradiologia.Application.Common;

namespace Teleradiologia.Application.Informes;

public interface IInformeService
{
    Task<InformeResponse> CrearAsync(Guid estudioId, Guid radiologoId, CrearInformeRequest request, CancellationToken ct);

    Task<InformeResponse> EditarAsync(Guid informeId, Guid radiologoId, EditarInformeRequest request, CancellationToken ct);

    Task<InformeResponse> FirmarAsync(Guid informeId, Guid radiologoId, FirmarInformeRequest request, CancellationToken ct);

    Task<InformeResponse> CrearAdendaAsync(Guid informeAnteriorId, Guid radiologoId, CrearInformeRequest request, CancellationToken ct);

    Task<IReadOnlyList<InformeResponse>> GetByEstudioAsync(Guid estudioId, CancellationToken ct);

    Task<PagedResult<InformeListadoResponse>> BuscarAsync(FiltroInformes filtro, CancellationToken ct);

    // `filtro` acarrea el alcance del rol: quien no tenga derecho a este informe recibe
    // el mismo 404 que si no existiera.
    Task<InformeDetalleResponse> ObtenerParaLecturaAsync(Guid informeId, FiltroInformes alcance, CancellationToken ct);

    Task<VerificacionFirmaResponse> VerificarFirmaAsync(Guid informeId, CancellationToken ct);
}
