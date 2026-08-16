using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Infrastructure.Persistence;

public static class RegistroDeEventos
{
    // Campos que no se copian a la bitácora: el contenido de un informe es texto clínico y
    // duplicarlo acá multiplicaría los datos de salud sin agregar nada al control de cambios.
    private static readonly HashSet<string> Redactados = new(StringComparer.Ordinal)
    {
        nameof(Informe.Contenido),
    };

    // Ruido de mantenimiento: quién y cuándo ya son columnas del propio evento.
    private static readonly HashSet<string> Ignorados = new(StringComparer.Ordinal)
    {
        "CreatedAt",
        "CreatedBy",
        "LastModified",
        "LastModifiedBy",
    };

    public static List<Evento> Capturar(ChangeTracker tracker, Guid? usuarioId, string? usuarioEmail, DateTimeOffset ahora)
    {
        var eventos = new List<Evento>();

        foreach (var entry in tracker.Entries())
        {
            // Las bitácoras no se auditan a sí mismas.
            if (entry.Entity is Evento or AuditLog)
            {
                continue;
            }

            var operacion = entry.State switch
            {
                EntityState.Added => TipoOperacion.Creacion,
                EntityState.Modified => TipoOperacion.Modificacion,
                EntityState.Deleted => TipoOperacion.Eliminacion,
                _ => (TipoOperacion?)null,
            };

            if (operacion is null)
            {
                continue;
            }

            var cambios = operacion switch
            {
                TipoOperacion.Creacion => ValoresDe(entry, p => p.CurrentValue),
                TipoOperacion.Eliminacion => ValoresDe(entry, p => p.OriginalValue),
                _ => Diferencias(entry),
            };

            // Un UPDATE que solo tocó campos ignorados no es un cambio que valga registrar.
            if (operacion == TipoOperacion.Modificacion && cambios.Count == 0)
            {
                continue;
            }

            eventos.Add(new Evento
            {
                Id = Guid.NewGuid(),
                Entidad = entry.Metadata.ClrType.Name,
                EntidadId = ClavePrimaria(entry),
                Operacion = operacion.Value,
                UsuarioId = usuarioId,
                UsuarioEmail = usuarioEmail,
                Cambios = cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios),
                Timestamp = ahora,
            });
        }

        return eventos;
    }

    private static Dictionary<string, object?> ValoresDe(EntityEntry entry, Func<PropertyEntry, object?> tomar)
    {
        var valores = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var propiedad in entry.Properties)
        {
            var nombre = propiedad.Metadata.Name;

            if (Ignorados.Contains(nombre) || propiedad.Metadata.IsPrimaryKey())
            {
                continue;
            }

            var valor = tomar(propiedad);
            if (valor is null)
            {
                continue;
            }

            valores[nombre] = Redactados.Contains(nombre) ? "(omitido)" : Texto(valor);
        }

        return valores;
    }

    private static Dictionary<string, object?> Diferencias(EntityEntry entry)
    {
        var cambios = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var propiedad in entry.Properties)
        {
            var nombre = propiedad.Metadata.Name;

            if (!propiedad.IsModified || Ignorados.Contains(nombre))
            {
                continue;
            }

            if (Equals(propiedad.OriginalValue, propiedad.CurrentValue))
            {
                continue;
            }

            cambios[nombre] = Redactados.Contains(nombre)
                ? new { antes = "(omitido)", despues = "(omitido)" }
                : new { antes = Texto(propiedad.OriginalValue), despues = Texto(propiedad.CurrentValue) };
        }

        return cambios;
    }

    private static string ClavePrimaria(EntityEntry entry)
    {
        var clave = entry.Metadata.FindPrimaryKey();
        if (clave is null)
        {
            return string.Empty;
        }

        var partes = clave.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);

        return string.Join('|', partes);
    }

    private static string? Texto(object? valor) => valor switch
    {
        null => null,
        DateTimeOffset fecha => fecha.ToString("O"),
        DateTime fecha => fecha.ToString("O"),
        _ => valor.ToString(),
    };
}
