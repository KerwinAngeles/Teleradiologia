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
    IFirmaDigitalService firmaDigital,
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

    public async Task<InformeResponse> FirmarAsync(Guid informeId, Guid radiologoId, FirmarInformeRequest request, CancellationToken ct)
    {
        var informe = await ObtenerPropioAsync(informeId, radiologoId, ct);

        if (informe.Estado != EstadoInforme.Borrador)
        {
            throw new EstadoInformeInvalidoException("Este informe ya está firmado.");
        }

        var estudio = await estudioRepository.GetByIdAsync(informe.EstudioId, ct)
            ?? throw new EstudioNoEncontradoException(informe.EstudioId);

        var radiologo = await identityService.ObtenerPorIdAsync(radiologoId, ct);

        informe.Estado = EstadoInforme.Firmado;
        informe.FirmadoAt = TruncarAMicrosegundos(DateTimeOffset.UtcNow);
        informe.FirmanteNombre = radiologo?.NombreCompleto;
        informe.FirmanteMatricula = radiologo?.Matricula;
        informe.FirmaImagen = ValidarTrazo(request.FirmaImagen);
        informe.VersionFirma = PayloadFirma.VersionActual;

        var firma = firmaDigital.Firmar(
            PayloadFirma.Construir(informe, estudio, PayloadFirma.VersionActual));
        informe.HashContenido = firma.Hash;
        informe.Firma = firma.Firma;
        informe.AlgoritmoFirma = firma.Algoritmo;

        // Solo el informe original cierra el estudio — una adenda no lo vuelve a mover de Informado.
        if (informe.InformeAnteriorId is null)
        {
            estudio.Estado = EstadoEstudio.Informado;
            // Cierra el reloj del SLA: es el momento contra el que se mide la entrega.
            estudio.InformadoAt = informe.FirmadoAt;
        }

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = radiologoId,
            EstudioId = informe.EstudioId,
            Accion = TipoAccionAuditoria.FirmoInforme,
        });

        await unitOfWork.SaveChangesAsync(ct);

        await NotificarHospitalAsync(estudio, ct);

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
        // Un estudio de otro hospital tiene que dar 404, igual que pedirlo directo, y no una lista vacía.
        _ = await estudioRepository.GetByIdAsync(estudioId, ct)
            ?? throw new EstudioNoEncontradoException(estudioId);

        var informes = await informeRepository.GetByEstudioAsync(estudioId, ct);

        var nombres = new UsuarioNombreCache(identityService);
        var resultado = new List<InformeResponse>(informes.Count);
        foreach (var informe in informes)
        {
            resultado.Add(await MapearAsync(informe, nombres, ct));
        }

        return resultado;
    }

    private const int MaximoBytesTrazo = 300 * 1024;

    private static string? ValidarTrazo(string? firmaImagen)
    {
        if (string.IsNullOrWhiteSpace(firmaImagen))
        {
            return null;
        }

        if (!firmaImagen.StartsWith("data:image/png;base64,", StringComparison.Ordinal))
        {
            throw new ArchivoDicomInvalidoException("La firma manuscrita debe ser un PNG.");
        }

        if (firmaImagen.Length > MaximoBytesTrazo)
        {
            throw new ArchivoDicomInvalidoException("La firma manuscrita es demasiado grande.");
        }

        return firmaImagen;
    }

    // 10 ticks = 1 microsegundo: lo que la base es capaz de conservar.
    private static DateTimeOffset TruncarAMicrosegundos(DateTimeOffset instante) =>
        new(instante.Ticks - (instante.Ticks % 10), instante.Offset);

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
            informe.FirmadoAt,
            informe.HashContenido,
            informe.AlgoritmoFirma,
            informe.FirmanteNombre,
            informe.FirmanteMatricula,
            informe.FirmaImagen);

    public async Task<VerificacionFirmaResponse> VerificarFirmaAsync(Guid informeId, CancellationToken ct)
    {
        var informe = await informeRepository.GetByIdAsync(informeId, ct)
            ?? throw new InformeNoEncontradoException(informeId);

        var estudio = await estudioRepository.GetByIdAsync(informe.EstudioId, ct)
            ?? throw new EstudioNoEncontradoException(informe.EstudioId);

        if (!informe.EstaFirmado)
        {
            return new VerificacionFirmaResponse(
                informe.Id, false, false, false,
                "El informe todavía no está firmado.",
                informe.HashContenido, string.Empty, informe.AlgoritmoFirma,
                informe.FirmanteNombre, informe.FirmanteMatricula, informe.FirmadoAt);
        }

        // Se recalcula sobre el contenido ACTUAL: si alguien lo editó por fuera de la
        // aplicación, el hash deja de coincidir y la verificación falla.
        // Se reconstruye con la versión con la que se firmó: los informes viejos siguen
        // verificándose aunque el formato del payload haya cambiado después.
        var resultado = firmaDigital.Verificar(
            PayloadFirma.Construir(informe, estudio, informe.VersionFirma ?? 1),
            informe.HashContenido,
            informe.Firma);

        var motivo = resultado switch
        {
            { HashCoincide: false } => "El contenido del informe no coincide con el que se firmó.",
            { FirmaValida: false } => "La firma no corresponde a la clave de la plataforma.",
            _ => null,
        };

        return new VerificacionFirmaResponse(
            informe.Id,
            resultado.EsValida,
            resultado.HashCoincide,
            resultado.FirmaValida,
            motivo,
            informe.HashContenido,
            resultado.HashCalculado,
            informe.AlgoritmoFirma,
            informe.FirmanteNombre,
            informe.FirmanteMatricula,
            informe.FirmadoAt);
    }
}
