namespace Teleradiologia.Application.Common.Exceptions;

public sealed class UsuarioInvalidoException(IReadOnlyList<string> errores)
    : AppException(string.Join(" ", errores))
{
    public IReadOnlyList<string> Errores { get; } = errores;
}
