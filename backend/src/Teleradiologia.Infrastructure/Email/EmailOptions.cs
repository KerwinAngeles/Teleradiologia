namespace Teleradiologia.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 1025;

    // SslOnConnect | StartTls | None | Auto. Auto deduce del puerto: 465 es TLS directo,
    // cualquier otro es STARTTLS. Solo hace falta fijarlo para un servidor sin TLS (None).
    public string ModoTls { get; set; } = "Auto";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@teleradiologia.local";

    public string FromName { get; set; } = "Teleradiología";
}
