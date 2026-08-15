namespace Teleradiologia.Application.Common.Exceptions;

public sealed class EstadoInformeInvalidoException(string mensaje) : AppException(mensaje);
