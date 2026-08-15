namespace Teleradiologia.Application.Common;

public class BaseResponse<T>
{
    public bool HasError { get; set; }

    public string? Error { get; set; }

    public T? Data { get; set; }

    public ErrorCode? Code { get; set; }

    public static BaseResponse<T> Success(T data) => new() { HasError = false, Data = data };

    public static BaseResponse<T> Fail(string error, ErrorCode code = ErrorCode.Invalido) =>
        new() { HasError = true, Error = error, Code = code };
}

public enum ErrorCode
{
    Invalido,
    NoAutenticado,
    Prohibido,
    NoEncontrado,
    Conflicto,
    ServicioExterno,
}
