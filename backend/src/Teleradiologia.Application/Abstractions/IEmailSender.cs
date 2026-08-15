namespace Teleradiologia.Application.Abstractions;

public interface IEmailSender
{
    Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoTexto, CancellationToken ct);
}
