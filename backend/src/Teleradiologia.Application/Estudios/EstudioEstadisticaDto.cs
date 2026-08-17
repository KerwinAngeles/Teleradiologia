using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

// Proyección mínima para los KPIs y gráficos: van sobre el total, no sobre la página visible.
// Con decenas de miles de estudios habría que pasar a agregación en SQL.
// Las marcas de tiempo viajan crudas: el frontend deriva de ellas los tiempos de
// espera, de lectura y de vuelta completa sin pedir un endpoint por métrica.
public record EstudioEstadisticaDto(
    EstadoEstudio Estado,
    PrioridadEstudio Prioridad,
    string Modalidad,
    string HospitalNombre,
    Guid SubidoPorId,
    Guid? RadiologoAsignadoId,
    bool Vencido,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AsignadoAt,
    DateTimeOffset? InformadoAt,
    DateTimeOffset FechaLimite);
