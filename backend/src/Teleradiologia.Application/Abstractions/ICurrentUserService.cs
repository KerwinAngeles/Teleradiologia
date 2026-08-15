namespace Teleradiologia.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UsuarioId { get; }

    string? Email { get; }
}
