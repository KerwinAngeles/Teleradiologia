namespace Teleradiologia.Application.Common.Exceptions;

public sealed class ArchivoDicomInvalidoException(string mensaje) : AppException(mensaje);
