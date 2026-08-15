using Microsoft.AspNetCore.Diagnostics;
using Teleradiologia.Application.Common.Exceptions;

namespace Teleradiologia.Api.ExceptionHandling;

public class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, mensaje) = exception switch
        {
            CredencialesInvalidasException => (StatusCodes.Status401Unauthorized, exception.Message),
            EmailYaRegistradoException => (StatusCodes.Status409Conflict, exception.Message),
            UsuarioInvalidoException e => (StatusCodes.Status400BadRequest, string.Join(" ", e.Errores)),
            ArchivoDicomInvalidoException => (StatusCodes.Status400BadRequest, exception.Message),
            EstudioNoEncontradoException => (StatusCodes.Status404NotFound, exception.Message),
            EstudioNoDisponibleException => (StatusCodes.Status409Conflict, exception.Message),
            InformeNoEncontradoException => (StatusCodes.Status404NotFound, exception.Message),
            ProhibidoException => (StatusCodes.Status403Forbidden, exception.Message),
            EstadoInformeInvalidoException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (0, string.Empty),
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message = mensaje }, ct);
        return true;
    }
}
