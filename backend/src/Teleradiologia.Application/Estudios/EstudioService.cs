using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
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
    IUnitOfWork unitOfWork) : IEstudioService
{
    public async Task<SubirEstudioResultado> SubirEstudioAsync(SubirEstudioRequest request, CancellationToken ct)
    {
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

        // Idempotente: ante un reintento se devuelve el estudio ya existente.
        var estudioExistente = await estudioRepository.GetByStudyInstanceUidAsync(metadata.StudyInstanceUid, ct);
        if (estudioExistente is not null)
        {
            return new SubirEstudioResultado(await MapearAsync(estudioExistente, ct), CreadoAhora: false);
        }

        var paciente = await pacienteRepository.GetByDocumentoAsync(metadata.PacienteDocumento, ct);
        if (paciente is null)
        {
            paciente = new Paciente
            {
                Id = Guid.NewGuid(),
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
            HospitalOrigen = request.HospitalOrigen,
            FechaEstudio = metadata.FechaEstudio ?? DateTimeOffset.UtcNow,
            Estado = EstadoEstudio.Pendiente,
            SubidoPorId = request.SubidoPorId,
        };
        estudioRepository.Add(estudio);

        await unitOfWork.SaveChangesAsync(ct);

        return new SubirEstudioResultado(await MapearAsync(estudio, ct), CreadoAhora: true);
    }

    public async Task<IReadOnlyList<EstudioResponse>> GetAllAsync(EstadoEstudio? estado, Guid? soloAsignadosAUsuario, CancellationToken ct)
    {
        var estudios = await estudioRepository.GetAllAsync(estado, soloAsignadosAUsuario, ct);

        var nombres = new UsuarioNombreCache(identityService);
        var resultado = new List<EstudioResponse>(estudios.Count);
        foreach (var estudio in estudios)
        {
            resultado.Add(await MapearAsync(estudio, nombres, ct));
        }

        return resultado;
    }

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

    private static async Task<EstudioResponse> MapearAsync(Estudio estudio, UsuarioNombreCache nombres, CancellationToken ct)
    {
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
            estudio.HospitalOrigen,
            estudio.FechaEstudio,
            estudio.Estado,
            estudio.RadiologoAsignadoId,
            nombreRadiologo,
            estudio.SubidoPorId,
            nombreSubioPor,
            estudio.CreatedAt);
    }
}
