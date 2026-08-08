using Microsoft.EntityFrameworkCore;

namespace Teleradiologia.Api.Data;

/// <summary>
/// DbContext principal de la plataforma. Vacío por ahora — las entidades
/// (Paciente, Estudio, Informe, Usuario, AuditLog, ...) se agregan feature por feature.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
