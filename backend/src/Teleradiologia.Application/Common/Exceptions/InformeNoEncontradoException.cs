namespace Teleradiologia.Application.Common.Exceptions;

public sealed class InformeNoEncontradoException(Guid informeId) : AppException($"No existe el informe '{informeId}'.");
