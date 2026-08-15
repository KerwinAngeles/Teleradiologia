using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure;

public static class InfrastructureStartup
{
    // Database first: el esquema es db/schema.sql. Acá solo se valida que esté aplicado.
    public static async Task VerificarBaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILogger<AppDbContext>>();

        if (!await db.Database.CanConnectAsync())
        {
            logger.LogError("No se pudo conectar a la base de datos.");
            return;
        }

        var faltantes = new List<string>();

        foreach (var tabla in (string[])["Usuarios", "Pacientes", "Estudios", "Informes", "AuditLogs"])
        {
            // Las comillas importan: sin ellas Postgres baja el identificador a minúsculas
            // y no encuentra "Usuarios".
            var existe = await db.Database
                .SqlQuery<bool>($"SELECT to_regclass({$"public.\"{tabla}\""}) IS NOT NULL AS \"Value\"")
                .SingleAsync();

            if (!existe)
            {
                faltantes.Add(tabla);
            }
        }

        if (faltantes.Count > 0)
        {
            logger.LogError(
                "Faltan tablas en la base: {Tablas}. Aplicá db/schema.sql antes de usar la Api.",
                string.Join(", ", faltantes));
        }
    }
}
