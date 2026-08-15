namespace Teleradiologia.Application.Common.Exceptions;

public sealed class EmailYaRegistradoException(string email) : AppException($"Ya existe un usuario con el email '{email}'.");
