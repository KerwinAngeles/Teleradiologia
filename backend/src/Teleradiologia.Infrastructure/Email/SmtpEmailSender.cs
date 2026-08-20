using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Teleradiologia.Application.Abstractions;

namespace Teleradiologia.Infrastructure.Email;

public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task EnviarAsync(string destinatarioEmail, string asunto, string cuerpoTexto, CancellationToken ct)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mensaje.To.Add(MailboxAddress.Parse(destinatarioEmail));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpoTexto };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, ResolverModoTls(), ct);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);
            }

            await client.SendAsync(mensaje, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: no debe tumbar la operación que la disparó.
            logger.LogError(ex, "No se pudo enviar el email a {Destinatario}: {Asunto}", destinatarioEmail, asunto);
        }
    }

    // El 465 arranca en TLS; el 587 y el 2525 conectan en claro y suben con STARTTLS. Un
    // proveedor que solo ofrezca 587 quedaba fuera mientras esto era un booleano.
    private SecureSocketOptions ResolverModoTls() => _options.ModoTls.ToLowerInvariant() switch
    {
        "sslonconnect" => SecureSocketOptions.SslOnConnect,
        "starttls" => SecureSocketOptions.StartTls,
        "none" => SecureSocketOptions.None,
        _ => _options.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
    };
}
