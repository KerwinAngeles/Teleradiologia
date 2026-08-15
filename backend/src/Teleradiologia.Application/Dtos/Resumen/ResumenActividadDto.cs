namespace Teleradiologia.Application.Dtos.Resumen;

public record ResumenActividadDto(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    int EstudiosRecibidos,
    int InformesFirmados,
    int AdendasFirmadas,
    int EstudiosInformados,
    int EstudiosPendientes,
    int EstudiosEnInforme,
    IReadOnlyList<FirmasPorRadiologoDto> PorRadiologo)
{
    public bool SinActividad => EstudiosRecibidos == 0 && InformesFirmados == 0 && AdendasFirmadas == 0;
}

public record FirmasPorRadiologoDto(string Radiologo, int Firmados);
