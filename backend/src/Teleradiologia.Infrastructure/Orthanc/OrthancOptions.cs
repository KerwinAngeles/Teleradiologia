namespace Teleradiologia.Infrastructure.Orthanc;

public class OrthancOptions
{
    public const string SectionName = "Orthanc";

    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
