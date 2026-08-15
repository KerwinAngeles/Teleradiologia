using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Common;

namespace Teleradiologia.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string? DireccionIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    protected IActionResult Resultado<T>(BaseResponse<T> respuesta) =>
        respuesta.HasError
            ? Problem(detail: respuesta.Error, statusCode: EstadoHttp(respuesta.Code))
            : Ok(respuesta.Data);

    protected IActionResult Resultado<T>(BaseResponse<T> respuesta, Func<T, IActionResult> alCrear) =>
        respuesta.HasError
            ? Problem(detail: respuesta.Error, statusCode: EstadoHttp(respuesta.Code))
            : alCrear(respuesta.Data!);

    private static int EstadoHttp(ErrorCode? code) => code switch
    {
        ErrorCode.NoAutenticado => StatusCodes.Status401Unauthorized,
        ErrorCode.Prohibido => StatusCodes.Status403Forbidden,
        ErrorCode.NoEncontrado => StatusCodes.Status404NotFound,
        ErrorCode.Conflicto => StatusCodes.Status409Conflict,
        ErrorCode.ServicioExterno => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status400BadRequest,
    };
}
