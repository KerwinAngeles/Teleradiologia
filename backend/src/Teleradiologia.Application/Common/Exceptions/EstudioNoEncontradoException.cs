namespace Teleradiologia.Application.Common.Exceptions;

public sealed class EstudioNoEncontradoException(Guid estudioId) : AppException($"No existe el estudio '{estudioId}'.");
