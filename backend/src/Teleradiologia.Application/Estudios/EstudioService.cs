using Teleradiologia.Application.Abstractions;
using Microsoft.Extensions.Options;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Common.Exceptions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Estudios;

public class EstudioService(
    IOrthancClient orthancClient,
    IIdentityService identityService,
    IPacienteRepository pacienteRepository,
    IEstudioRepository estudioRepository,
    IAuditLogRepository auditLogRepository,
    IHospitalRepository hospitalRepository,
    IOptions<SlaOptions> slaOptions,
    IUsuarioRepository usuarioRepository,
    INotificacionRepository notificacionRepository,
    INotificadorTiempoReal notificador,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IEstudioService
{
    public async Task<SubirEstudioResultado> SubirEstudioAsync(SubirEstudioRequest request, CancellationToken ct)
    {
        if (!tenant.PuedeVer(request.HospitalId))
        {
            throw new ProhibidoException("No estás habilitado para subir estudios a ese hospital.");
        }

        if (request.ArchivosDicom.Count == 0)
        {
            throw new ArchivoDicomInvalidoException("No se recibió ningún archivo DICOM.");
        }

        // Los tags del estudio se repiten en cada instancia: alcanza con la primera.
        OrthancInstanciaSubida? primeraInstancia = null;
        foreach (var archivo in request.ArchivosDicom)
        {
            var instancia = await orthancClient.SubirInstanciaAsync(archivo, ct);
            primeraInstancia ??= instancia;
        }

        var metadata = await orthancClient.ObtenerMetadataInstanciaAsync(primeraInstancia!.OrthancInstanceId, ct);

        // Idempotente dentro del hospital de destino: ante un reintento se devuelve el que ya está.
        var estudioExistente = await estudioRepository.GetExistenteAsync(
            request.HospitalId, primeraInstancia.OrthancStudyId, metadata.StudyInstanceUid, ct);
        if (estudioExistente is not null)
        {
            return new SubirEstudioResultado(await MapearAsync(estudioExistente, ct), CreadoAhora: false);
        }

        var paciente = await pacienteRepository.GetByDocumentoAsync(request.HospitalId, metadata.PacienteDocumento, ct);
        if (paciente is null)
        {
            paciente = new Paciente
            {
                Id = Guid.NewGuid(),
                HospitalId = request.HospitalId,
                NombreCompleto = metadata.PacienteNombre,
                DocumentoIdentidad = metadata.PacienteDocumento,
                FechaNacimiento = metadata.PacienteFechaNacimiento ?? default,
                Sexo = metadata.PacienteSexo ?? SexoPaciente.Otro,
            };
            pacienteRepository.Add(paciente);
        }

        var estudio = new Estudio
        {
            Id = Guid.NewGuid(),
            PacienteId = paciente.Id,
            Paciente = paciente,
            OrthancStudyId = primeraInstancia.OrthancStudyId,
            StudyInstanceUid = metadata.StudyInstanceUid,
            Modalidad = metadata.Modalidad,
            DescripcionEstudio = metadata.DescripcionEstudio,
            HospitalId = request.HospitalId,
            FechaEstudio = metadata.FechaEstudio ?? DateTimeOffset.UtcNow,
            Estado = EstadoEstudio.Pendiente,
            Prioridad = request.Prioridad,
            SubidoPorId = request.SubidoPorId,
        };

        var hospital = await hospitalRepository.GetByIdAsync(request.HospitalId, ct);
        estudio.FechaLimite = Plazos.CalcularLimite(
            DateTimeOffset.UtcNow, hospital, request.Prioridad, slaOptions.Value);
        estudioRepository.Add(estudio);

        var avisados = await PrepararNotificacionesAsync(estudio, ct);

        await unitOfWork.SaveChangesAsync(ct);

        await AvisarEnVivoAsync(estudio, avisados, ct);

        return new SubirEstudioResultado(await MapearAsync(estudio, ct), CreadoAhora: true);
    }

    public async Task<PagedResult<EstudioResponse>> BuscarAsync(FiltroEstudios filtro, CancellationToken ct)
    {
        var pagina = await estudioRepository.BuscarAsync(filtro, ct);

        var nombres = new UsuarioNombreCache(identityService);
        var items = new List<EstudioResponse>(pagina.Items.Count);
        foreach (var estudio in pagina.Items)
        {
            items.Add(await MapearAsync(estudio, nombres, ct));
        }

        return new PagedResult<EstudioResponse>(items, pagina.PageNumber, pagina.PageSize, pagina.TotalCount);
    }

    public async Task<IReadOnlyList<EstudioEstadisticaDto>> ObtenerEstadisticasAsync(CancellationToken ct) =>
        await estudioRepository.ProyectarEstadisticasAsync(ct);

    public async Task<EstudioResponse> TomarEstudioAsync(Guid estudioId, Guid radiologoId, CancellationToken ct)
    {
        var estudio = await estudioRepository.GetByIdAsync(estudioId, ct)
            ?? throw new EstudioNoEncontradoException(estudioId);

        if (estudio.Estado != EstadoEstudio.Pendiente || estudio.RadiologoAsignadoId is not null)
        {
            throw new EstudioNoDisponibleException(estudioId);
        }

        estudio.RadiologoAsignadoId = radiologoId;
        estudio.Estado = EstadoEstudio.EnInforme;
        estudio.AsignadoAt = DateTimeOffset.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);

        return await MapearAsync(estudio, ct);
    }

    public async Task<EstudioResponse> ObtenerPorIdAsync(Guid estudioId, CancellationToken ct)
    {
        var estudio = await estudioRepository.GetByIdAsync(estudioId, ct)
            ?? throw new EstudioNoEncontradoException(estudioId);

        return await MapearAsync(estudio, ct);
    }

    public async Task<IReadOnlyList<ImagenEstudioResponse>> ObtenerImagenesAsync(Guid estudioId, Guid usuarioId, CancellationToken ct)
    {
        var estudio = await estudioRepository.GetByIdAsync(estudioId, ct)
            ?? throw new EstudioNoEncontradoException(estudioId);

        var instancias = await orthancClient.ObtenerInstanciasDelEstudioAsync(estudio.OrthancStudyId, ct);

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            EstudioId = estudioId,
            Accion = TipoAccionAuditoria.VioEstudio,
        });
        await unitOfWork.SaveChangesAsync(ct);

        return instancias.Select(i => new ImagenEstudioResponse(i.OrthancInstanceId, i.NumeroInstancia)).ToList();
    }

    public async Task<(byte[] Bytes, string ContentType)> ObtenerImagenAsync(Guid estudioId, string orthancInstanceId, CancellationToken ct)
    {
        // No valida que la instancia sea de este estudio: todavía no hay ACL por hospital/paciente.
        _ = await estudioRepository.GetByIdAsync(estudioId, ct) ?? throw new EstudioNoEncontradoException(estudioId);

        var imagen = await orthancClient.ObtenerImagenInstanciaAsync(orthancInstanceId, ct);
        return (imagen.Bytes, imagen.ContentType);
    }

    public async Task<byte[]> ObtenerArchivoDicomAsync(Guid estudioId, string orthancInstanceId, CancellationToken ct)
    {
        _ = await estudioRepository.GetByIdAsync(estudioId, ct) ?? throw new EstudioNoEncontradoException(estudioId);

        return await orthancClient.ObtenerArchivoDicomAsync(orthancInstanceId, ct);
    }

    private Task<EstudioResponse> MapearAsync(Estudio estudio, CancellationToken ct) =>
        MapearAsync(estudio, new UsuarioNombreCache(identityService), ct);

    private async Task<EstudioResponse> MapearAsync(Estudio estudio, UsuarioNombreCache nombres, CancellationToken ct)
    {
        var ahora = DateTimeOffset.UtcNow;
        var nombreRadiologo = estudio.RadiologoAsignadoId is { } radiologoId
            ? await nombres.ObtenerAsync(radiologoId, ct)
            : null;
        var nombreSubioPor = await nombres.ObtenerAsync(estudio.SubidoPorId, ct);

        return new EstudioResponse(
            estudio.Id,
            estudio.Paciente.NombreCompleto,
            estudio.Paciente.DocumentoIdentidad,
            estudio.Modalidad,
            estudio.DescripcionEstudio,
            estudio.HospitalId,
            estudio.Hospital?.Nombre ?? string.Empty,
            estudio.FechaEstudio,
            estudio.Estado,
            estudio.Prioridad,
            estudio.FechaLimite,
            Plazos.Evaluar(estudio, slaOptions.Value, ahora),
            Plazos.MinutosRestantes(estudio, ahora),
            estudio.AsignadoAt,
            estudio.InformadoAt,
            estudio.RadiologoAsignadoId,
            nombreRadiologo,
            estudio.SubidoPorId,
            nombreSubioPor,
            estudio.CreatedAt);
    }

    private async Task<List<Notificacion>> PrepararNotificacionesAsync(Estudio estudio, CancellationToken ct)
    {
        var radiologos = await usuarioRepository.GetRadiologosDeHospitalAsync(estudio.HospitalId, ct);
        if (radiologos.Count == 0)
        {
            return [];
        }

        var urgente = estudio.Prioridad != PrioridadEstudio.Rutina;
        var paciente = estudio.Paciente?.NombreCompleto ?? "paciente";

        var notificaciones = radiologos.Select(r => new Notificacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = r.Id,
            Tipo = urgente ? TipoNotificacion.EstudioUrgente : TipoNotificacion.EstudioNuevo,
            Titulo = urgente
                ? $"Estudio {estudio.Prioridad.ToString().ToUpperInvariant()} para evaluar"
                : "Nuevo estudio para evaluar",
            Mensaje = $"{paciente} — {estudio.Modalidad}. Vence {estudio.FechaLimite:dd/MM HH:mm}.",
            EstudioId = estudio.Id,
            HospitalId = estudio.HospitalId,
        }).ToList();

        notificacionRepository.AddRange(notificaciones);

        return notificaciones;
    }

    // Después de guardar: si el push falla, la notificación igual quedó persistida y el
    // radiólogo la ve al entrar.
    private async Task AvisarEnVivoAsync(Estudio estudio, List<Notificacion> notificaciones, CancellationToken ct)
    {
        if (notificaciones.Count == 0)
        {
            return;
        }

        var modelo = notificaciones[0];
        modelo.Estudio = estudio;

        await notificador.EnviarAsync(
            [.. notificaciones.Select(n => n.UsuarioId)],
            Services.NotificacionService.Mapear(modelo),
            ct);
    }
}
