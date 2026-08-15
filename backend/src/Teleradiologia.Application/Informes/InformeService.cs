using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Common.Exceptions;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Informes;

public class InformeService(
    IEstudioRepository estudioRepository,
    IInformeRepository informeRepository,
    IIdentityService identityService,
    IAuditLogRepository auditLogRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork) : IInformeService
{
    public async Task<InformeResponse> CrearAsync(Guid estudioId, Guid radiologoId, CrearInformeRequest request, CancellationToken ct)
    {
        var estudio = await estudioRepository.GetByIdAsync(estudioId, ct)
            ?? throw new EstudioNoEncontradoException(estudioId);

        if (estudio.RadiologoAsignadoId != radiologoId)
        {
            throw new ProhibidoException("Solo el radiólogo asignado puede redactar el informe de este estudio.");
        }

        if (estudio.Estado != EstadoEstudio.EnInforme)
        {
            throw new EstadoInformeInvalidoException(
                "El estudio no está en un estado que permita crear un informe (¿todavía está Pendiente? ¿ya está Informado?).");
        }

        if (await informeRepository.ExisteParaEstudioAsync(estudioId, ct))
        {
            throw new EstadoInformeInvalidoException(
                "Ya existe un informe para este estudio — para corregirlo después de firmado, usá una adenda.");
        }

        var informe = new Informe
        {
            Id = Guid.NewGuid(),
            EstudioId = estudioId,
            RadiologoId = radiologoId,
            Contenido = request.Contenido,
            Estado = EstadoInforme.Borrador,
        };
        informeRepository.Add(informe);

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = radiologoId,
            EstudioId = estudioId,
            Accion = TipoAccionAuditoria.CreoInforme,
        });

        await unitOfWork.SaveChangesAsync(ct);

        return await MapearAsync(informe, ct);
    }

    public async Task<InformeResponse> EditarAsync(Guid informeId, Guid radiologoId, EditarInformeRequest request, CancellationToken ct)
    {
        var informe = await ObtenerPropioAsync(informeId, radiologoId, ct);

        if (informe.Estado != EstadoInforme.Borrador)
        {
            throw new EstadoInformeInvalidoException("Un informe firmado no se puede editar — usá una adenda.");
        }

        informe.Contenido = request.Contenido;
        await unitOfWork.SaveChangesAsync(ct);

        return await MapearAsync(informe, ct);
    }

    public async Task<InformeResponse> FirmarAsync(Guid informeId, Guid radiologoId, CancellationToken ct)
    {
        var informe = await ObtenerPropioAsync(informeId, radiologoId, ct);

        if (informe.Estado != EstadoInforme.Borrador)
        {
            throw new EstadoInformeInvalidoException("Este informe ya está firmado.");
        }

        informe.Estado = EstadoInforme.Firmado;
        informe.FirmadoAt = DateTimeOffset.UtcNow;

        var estudio = await estudioRepository.GetByIdAsync(informe.EstudioId, ct);

        // Solo el informe original cierra el estudio — una adenda no lo vuelve a mover de Informado.
        if (informe.InformeAnteriorId is null && estudio is not null)
        {
            estudio.Estado = EstadoEstudio.Informado;
        }

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = radiologoId,
            EstudioId = informe.EstudioId,
            Accion = TipoAccionAuditoria.FirmoInforme,
        });

        await unitOfWork.SaveChangesAsync(ct);

        if (estudio is not null)
        {
            await NotificarHospitalAsync(estudio, ct);
        }

        return await MapearAsync(informe, ct);
    }

    public async Task<InformeResponse> CrearAdendaAsync(Guid informeAnteriorId, Guid radiologoId, CrearInformeRequest request, CancellationToken ct)
    {
        var informeAnterior = await informeRepository.GetByIdAsync(informeAnteriorId, ct)
            ?? throw new InformeNoEncontradoException(informeAnteriorId);

        if (informeAnterior.Estado != EstadoInforme.Firmado)
        {
            throw new EstadoInformeInvalidoException("Solo se puede agregar una adenda sobre un informe firmado.");
        }

        var estudio = await estudioRepository.GetByIdAsync(informeAnterior.EstudioId, ct)
            ?? throw new EstudioNoEncontradoException(informeAnterior.EstudioId);

        if (estudio.RadiologoAsignadoId != radiologoId)
        {
            throw new ProhibidoException("Solo el radiólogo asignado puede agregar una adenda a este estudio.");
        }

        var adenda = new Informe
        {
            Id = Guid.NewGuid(),
            EstudioId = informeAnterior.EstudioId,
            RadiologoId = radiologoId,
            Contenido = request.Contenido,
            Estado = EstadoInforme.Borrador,
            InformeAnteriorId = informeAnterior.Id,
        };
        informeRepository.Add(adenda);

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = radiologoId,
            EstudioId = adenda.EstudioId,
            Accion = TipoAccionAuditoria.CreoInforme,
        });

        await unitOfWork.SaveChangesAsync(ct);

        return await MapearAsync(adenda, ct);
    }

    public async Task<IReadOnlyList<InformeResponse>> GetByEstudioAsync(Guid estudioId, CancellationToken ct)
    {
        var informes = await informeRepository.GetByEstudioAsync(estudioId, ct);

        var nombres = new UsuarioNombreCache(identityService);
        var resultado = new List<InformeResponse>(informes.Count);
        foreach (var informe in informes)
        {
            resultado.Add(await MapearAsync(informe, nombres, ct));
        }

        return resultado;
    }

    private async Task<Informe> ObtenerPropioAsync(Guid informeId, Guid radiologoId, CancellationToken ct)
    {
        var informe = await informeRepository.GetByIdAsync(informeId, ct)
            ?? throw new InformeNoEncontradoException(informeId);

        if (informe.RadiologoId != radiologoId)
        {
            throw new ProhibidoException("Solo el radiólogo autor puede operar sobre este informe.");
        }

        return informe;
    }

    private async Task NotificarHospitalAsync(Estudio estudio, CancellationToken ct)
    {
        var tecnico = await identityService.ObtenerPorIdAsync(estudio.SubidoPorId, ct);
        if (tecnico is null)
        {
            return;
        }

        var asunto = $"Informe listo — {estudio.Paciente.NombreCompleto} ({estudio.Modalidad})";
        var cuerpo =
            $"El informe del estudio de {estudio.Paciente.NombreCompleto} (doc. {estudio.Paciente.DocumentoIdentidad}), " +
            $"{estudio.Modalidad} del {estudio.FechaEstudio:d}, ya está firmado y disponible en la plataforma.";

        // Best-effort: un fallo de email no invalida la firma ya guardada.
        await emailSender.EnviarAsync(tecnico.Email, asunto, cuerpo, ct);
    }

    private Task<InformeResponse> MapearAsync(Informe informe, CancellationToken ct) =>
        MapearAsync(informe, new UsuarioNombreCache(identityService), ct);

    private static async Task<InformeResponse> MapearAsync(Informe informe, UsuarioNombreCache nombres, CancellationToken ct) =>
        new(
            informe.Id,
            informe.EstudioId,
            informe.RadiologoId,
            await nombres.ObtenerAsync(informe.RadiologoId, ct),
            informe.Contenido,
            informe.Estado,
            EsAdenda: informe.InformeAnteriorId is not null,
            informe.InformeAnteriorId,
            informe.CreatedAt,
            informe.FirmadoAt);
}
