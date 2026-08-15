namespace Teleradiologia.Application.Common.Exceptions;

public sealed class ProhibidoException(string mensaje) : AppException(mensaje);
