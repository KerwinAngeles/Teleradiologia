namespace Teleradiologia.Application.Common.Exceptions;

public sealed class EstudioNoDisponibleException(Guid estudioId)
    : AppException($"El estudio '{estudioId}' ya no está disponible para tomar.");
