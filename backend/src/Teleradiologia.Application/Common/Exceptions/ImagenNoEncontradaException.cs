namespace Teleradiologia.Application.Common.Exceptions;

// 404 y no 403 a propósito: un 403 confirmaría que la instancia existe en otro
// hospital, que es justamente lo que no debe poder averiguarse desde afuera.
public sealed class ImagenNoEncontradaException(string orthancInstanceId)
    : AppException($"No existe la imagen '{orthancInstanceId}' en este estudio.");
