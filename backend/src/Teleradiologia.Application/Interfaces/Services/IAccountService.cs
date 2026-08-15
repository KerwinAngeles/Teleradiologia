using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Interfaces.Services;

public interface IAccountService
{
    Task<BaseResponse<RegistroResponse>> RegistrarAsync(RegistroRequest request, CancellationToken ct);

    Task<BaseResponse<AutenticacionResponse>> LoginAsync(AutenticacionRequest request, string? direccionIp, CancellationToken ct);

    Task<BaseResponse<UsuarioDto>> ObtenerPerfilAsync(Guid usuarioId, CancellationToken ct);

    Task<BaseResponse<List<UsuarioDto>>> ListarAsync(EstadoAcceso? estado, CancellationToken ct);

    Task<BaseResponse<UsuarioDto>> AprobarAsync(Guid usuarioId, AprobarUsuarioRequest request, Guid adminId, CancellationToken ct);

    Task<BaseResponse<UsuarioDto>> RechazarAsync(Guid usuarioId, DecisionRequest request, Guid adminId, CancellationToken ct);

    Task<BaseResponse<UsuarioDto>> SuspenderAsync(Guid usuarioId, DecisionRequest request, Guid adminId, CancellationToken ct);

    Task<BaseResponse<UsuarioDto>> ReactivarAsync(Guid usuarioId, Guid adminId, CancellationToken ct);
}
