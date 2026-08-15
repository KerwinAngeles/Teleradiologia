namespace Teleradiologia.Workers.Options;

public class ResumenActividadOptions
{
    public const string SectionName = "Workers:ResumenActividad";

    public bool Habilitado { get; set; } = true;

    public int IntervaloHoras { get; set; } = 24;

    // Hora local del primer envío. Null arranca apenas levanta la aplicación.
    public int? HoraDeEnvio { get; set; } = 7;

    public bool EjecutarAlArrancar { get; set; }
}
