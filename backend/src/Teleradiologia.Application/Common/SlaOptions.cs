namespace Teleradiologia.Application.Common;

public class SlaOptions
{
    public const string SectionName = "Sla";

    public int StatMinutos { get; set; } = 30;

    public int UrgenteMinutos { get; set; } = 120;

    public int RutinaMinutos { get; set; } = 1440;

    // Porcentaje del plazo que, al quedar por debajo, marca el estudio como "por vencer".
    public int UmbralPorVencerPorcentaje { get; set; } = 25;
}
