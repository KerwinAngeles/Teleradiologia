using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Eventos;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;
using Teleradiologia.Infrastructure.Persistence;

namespace Teleradiologia.Infrastructure.Repositories;

public class EventoRepository(AppDbContext db) : IEventoRepository
{
    private const int TopAgrupaciones = 6;

    public async Task<PagedResult<Evento>> BuscarAsync(FiltroEventos filtro, CancellationToken ct)
    {
        var query = Filtrar(filtro);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(filtro.Skip)
            .Take(filtro.SafePageSize)
            .ToListAsync(ct);

        return new PagedResult<Evento>(items, filtro.SafePageNumber, filtro.SafePageSize, total);
    }

    public async Task<KpisEventosDto> ObtenerKpisAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        var query = db.Eventos.Where(e => e.Timestamp >= desde && e.Timestamp < hasta);

        var porOperacion = await query
            .GroupBy(e => e.Operacion)
            .Select(g => new { Operacion = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);

        var porEntidad = await query
            .GroupBy(e => e.Entidad)
            .Select(g => new { Clave = g.Key, Cantidad = g.Count() })
            .OrderByDescending(g => g.Cantidad)
            .Take(TopAgrupaciones)
            .ToListAsync(ct);

        var porUsuario = await query
            .Where(e => e.UsuarioEmail != null)
            .GroupBy(e => e.UsuarioEmail!)
            .Select(g => new { Clave = g.Key, Cantidad = g.Count() })
            .OrderByDescending(g => g.Cantidad)
            .Take(TopAgrupaciones)
            .ToListAsync(ct);

        var usuariosActivos = await query
            .Where(e => e.UsuarioId != null)
            .Select(e => e.UsuarioId)
            .Distinct()
            .CountAsync(ct);

        int Contar(TipoOperacion op) => porOperacion.FirstOrDefault(o => o.Operacion == op)?.Cantidad ?? 0;

        return new KpisEventosDto(
            desde,
            hasta,
            porOperacion.Sum(o => o.Cantidad),
            Contar(TipoOperacion.Creacion),
            Contar(TipoOperacion.Modificacion),
            Contar(TipoOperacion.Eliminacion),
            usuariosActivos,
            [.. porEntidad.Select(g => new ConteoPorClaveDto(g.Clave, g.Cantidad))],
            [.. porUsuario.Select(g => new ConteoPorClaveDto(g.Clave, g.Cantidad))]);
    }

    public Task<List<string>> GetEntidadesAsync(CancellationToken ct) =>
        db.Eventos
            .Select(e => e.Entidad)
            .Distinct()
            .OrderBy(nombre => nombre)
            .ToListAsync(ct);

    private IQueryable<Evento> Filtrar(FiltroEventos filtro)
    {
        var query = db.Eventos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            query = query.Where(e => e.Entidad == filtro.Entidad);
        }

        if (filtro.Operacion is { } operacion)
        {
            query = query.Where(e => e.Operacion == operacion);
        }

        if (filtro.UsuarioId is { } usuarioId)
        {
            query = query.Where(e => e.UsuarioId == usuarioId);
        }

        if (filtro.Desde is { } desde)
        {
            query = query.Where(e => e.Timestamp >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            query = query.Where(e => e.Timestamp < hasta);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = $"%{filtro.Texto.Trim()}%";
            query = query.Where(e =>
                (e.UsuarioEmail != null && EF.Functions.ILike(e.UsuarioEmail, texto)) ||
                EF.Functions.ILike(e.EntidadId, texto));
        }

        return query;
    }
}
