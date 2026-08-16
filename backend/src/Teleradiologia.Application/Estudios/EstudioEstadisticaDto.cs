using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

// Proyección mínima para los KPIs y gráficos: van sobre el total, no sobre la página visible.
// Con decenas de miles de estudios habría que pasar a agregación en SQL.
public record EstudioEstadisticaDto(
    EstadoEstudio Estado,
    PrioridadEstudio Prioridad,
    string Modalidad,
    string HospitalNombre,
    Guid SubidoPorId,
    Guid? RadiologoAsignadoId,
    bool Vencido);
