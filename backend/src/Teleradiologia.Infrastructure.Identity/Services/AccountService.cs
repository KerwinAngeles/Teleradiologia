using AutoMapper;
using Microsoft.Extensions.Logging;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Application.Interfaces.Auth;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Infrastructure.Identity.Services;

public class AccountService(
    IAuthProvider authProvider,
    IUsuarioRepository usuarioRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IMapper mapper,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<BaseResponse<RegistroResponse>> RegistrarAsync(RegistroRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await usuarioRepository.GetByEmailAsync(email, ct) is not null)
        {
            return BaseResponse<RegistroResponse>.Fail("Ese email ya está registrado.", ErrorCode.Conflicto);
        }

        // Base vacía: el primero que se registra es el Admin, si no nadie podría aprobar a nadie.
        var esPrimerUsuario = !await usuarioRepository.ExisteAlgunoAsync(ct);

        var creado = await authProvider.CrearUsuarioAsync(email, request.Password, ct);
        if (creado.HasError)
        {
            return BaseResponse<RegistroResponse>.Fail(creado.Error!, creado.Code ?? ErrorCode.ServicioExterno);
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreCompleto = request.NombreCompleto.Trim(),
            Email = email,
            Proveedor = authProvider.Nombre,
            ProveedorUserId = creado.Data!.ProveedorUserId,
            Rol = esPrimerUsuario ? RolUsuario.Admin : RolUsuario.Tecnico,
            EstadoAcceso = esPrimerUsuario ? EstadoAcceso.Aprobado : EstadoAcceso.Pendiente,
            FechaDecision = esPrimerUsuario ? DateTimeOffset.UtcNow : null,
        };

        usuarioRepository.Add(usuario);

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Accion = TipoAccionAuditoria.SeRegistro,
        });

        await unitOfWork.SaveChangesAsync(ct);

        if (!esPrimerUsuario)
        {
            await AvisarAdminsAsync(usuario, ct);
        }

        var mensaje = esPrimerUsuario
            ? "Cuenta creada como administrador. Ya podés iniciar sesión."
            : "Tu cuenta fue creada y espera que un administrador la habilite. Te vamos a avisar por email.";

        return BaseResponse<RegistroResponse>.Success(
            new RegistroResponse(usuario.Id, usuario.Email, usuario.EstadoAcceso, mensaje));
    }

    public async Task<BaseResponse<AutenticacionResponse>> LoginAsync(AutenticacionRequest request, string? direccionIp, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Credencial primero: así no se puede averiguar qué emails existen ni su estado.
        var sesion = await authProvider.IniciarSesionAsync(email, request.Password, ct);
        if (sesion.HasError)
        {
            return BaseResponse<AutenticacionResponse>.Fail(sesion.Error!, sesion.Code ?? ErrorCode.NoAutenticado);
        }

        var usuario = await usuarioRepository.GetByProveedorUserIdAsync(sesion.Data!.ProveedorUserId, ct)
            ?? await usuarioRepository.GetByEmailAsync(email, ct);

        if (usuario is null)
        {
            logger.LogWarning("Credencial válida en el proveedor sin perfil local: {Email}", email);
            return BaseResponse<AutenticacionResponse>.Fail("Tu cuenta no está habilitada en la plataforma.", ErrorCode.Prohibido);
        }

        if (!usuario.PuedeIniciarSesion)
        {
            return BaseResponse<AutenticacionResponse>.Fail(MensajeDeEstado(usuario), ErrorCode.Prohibido);
        }

        // Cuentas migradas desde Identity: se vinculan al proveedor en el primer login.
        if (string.IsNullOrEmpty(usuario.ProveedorUserId))
        {
            usuario.ProveedorUserId = sesion.Data.ProveedorUserId;
            usuario.Proveedor = authProvider.Nombre;
            usuarioRepository.Update(usuario);
        }

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Accion = TipoAccionAuditoria.IniciarSesion,
            DireccionIp = direccionIp,
        });

        await unitOfWork.SaveChangesAsync(ct);

        return BaseResponse<AutenticacionResponse>.Success(new AutenticacionResponse(
            sesion.Data.AccessToken,
            sesion.Data.RefreshToken,
            sesion.Data.ExpiresAt,
            mapper.Map<UsuarioDto>(usuario)));
    }

    public async Task<BaseResponse<UsuarioDto>> ObtenerPerfilAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await usuarioRepository.GetByIdAsync(usuarioId, ct);

        return usuario is null
            ? BaseResponse<UsuarioDto>.Fail("No existe el usuario.", ErrorCode.NoEncontrado)
            : BaseResponse<UsuarioDto>.Success(mapper.Map<UsuarioDto>(usuario));
    }

    public async Task<BaseResponse<List<UsuarioDto>>> ListarAsync(EstadoAcceso? estado, CancellationToken ct)
    {
        var usuarios = await usuarioRepository.GetByEstadoAsync(estado, ct);
        return BaseResponse<List<UsuarioDto>>.Success(mapper.Map<List<UsuarioDto>>(usuarios));
    }

    public Task<BaseResponse<UsuarioDto>> AprobarAsync(Guid usuarioId, AprobarUsuarioRequest request, Guid adminId, CancellationToken ct) =>
        DecidirAsync(
            usuarioId,
            adminId,
            EstadoAcceso.Aprobado,
            TipoAccionAuditoria.AproboUsuario,
            rol: request.Rol,
            motivo: null,
            estadosValidos: [EstadoAcceso.Pendiente, EstadoAcceso.Rechazado, EstadoAcceso.Suspendido],
            asunto: "Tu acceso a Teleradiología fue habilitado",
            cuerpo: u => $"""
                Hola {u.NombreCompleto},

                Un administrador habilitó tu cuenta con el rol de {u.Rol}.
                Ya podés iniciar sesión en la plataforma.
                """,
            ct);

    public Task<BaseResponse<UsuarioDto>> RechazarAsync(Guid usuarioId, DecisionRequest request, Guid adminId, CancellationToken ct) =>
        DecidirAsync(
            usuarioId,
            adminId,
            EstadoAcceso.Rechazado,
            TipoAccionAuditoria.RechazoUsuario,
            rol: null,
            motivo: request.Motivo,
            estadosValidos: [EstadoAcceso.Pendiente],
            asunto: "Tu solicitud de acceso a Teleradiología",
            cuerpo: u => $"""
                Hola {u.NombreCompleto},

                Tu solicitud de acceso no fue aprobada.
                {(string.IsNullOrWhiteSpace(u.MotivoDecision) ? "" : $"Motivo: {u.MotivoDecision}")}
                """,
            ct);

    public Task<BaseResponse<UsuarioDto>> SuspenderAsync(Guid usuarioId, DecisionRequest request, Guid adminId, CancellationToken ct) =>
        DecidirAsync(
            usuarioId,
            adminId,
            EstadoAcceso.Suspendido,
            TipoAccionAuditoria.SuspendioUsuario,
            rol: null,
            motivo: request.Motivo,
            estadosValidos: [EstadoAcceso.Aprobado],
            asunto: "Tu acceso a Teleradiología fue suspendido",
            cuerpo: u => $"""
                Hola {u.NombreCompleto},

                Un administrador suspendió tu acceso a la plataforma.
                {(string.IsNullOrWhiteSpace(u.MotivoDecision) ? "" : $"Motivo: {u.MotivoDecision}")}
                """,
            ct);

    public Task<BaseResponse<UsuarioDto>> ReactivarAsync(Guid usuarioId, Guid adminId, CancellationToken ct) =>
        DecidirAsync(
            usuarioId,
            adminId,
            EstadoAcceso.Aprobado,
            TipoAccionAuditoria.ReactivoUsuario,
            rol: null,
            motivo: null,
            estadosValidos: [EstadoAcceso.Suspendido],
            asunto: "Tu acceso a Teleradiología fue restablecido",
            cuerpo: u => $"""
                Hola {u.NombreCompleto},

                Tu acceso fue restablecido. Ya podés volver a iniciar sesión.
                """,
            ct);

    private async Task<BaseResponse<UsuarioDto>> DecidirAsync(
        Guid usuarioId,
        Guid adminId,
        EstadoAcceso nuevoEstado,
        TipoAccionAuditoria accion,
        RolUsuario? rol,
        string? motivo,
        IReadOnlyList<EstadoAcceso> estadosValidos,
        string asunto,
        Func<Usuario, string> cuerpo,
        CancellationToken ct)
    {
        var usuario = await usuarioRepository.GetByIdAsync(usuarioId, ct);
        if (usuario is null)
        {
            return BaseResponse<UsuarioDto>.Fail("No existe el usuario.", ErrorCode.NoEncontrado);
        }

        if (!estadosValidos.Contains(usuario.EstadoAcceso))
        {
            return BaseResponse<UsuarioDto>.Fail(
                $"No se puede pasar de {usuario.EstadoAcceso} a {nuevoEstado}.", ErrorCode.Conflicto);
        }

        if (usuario.Id == adminId && nuevoEstado != EstadoAcceso.Aprobado)
        {
            return BaseResponse<UsuarioDto>.Fail("No podés quitarte el acceso a vos mismo.", ErrorCode.Invalido);
        }

        usuario.EstadoAcceso = nuevoEstado;
        usuario.FechaDecision = DateTimeOffset.UtcNow;
        usuario.DecididoPorId = adminId;
        usuario.MotivoDecision = motivo;

        if (rol is not null)
        {
            usuario.Rol = rol.Value;
        }

        usuarioRepository.Update(usuario);

        auditLogRepository.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = adminId,
            Accion = accion,
        });

        await unitOfWork.SaveChangesAsync(ct);

        await EnviarSinRompirAsync(usuario.Email, asunto, cuerpo(usuario), ct);

        return BaseResponse<UsuarioDto>.Success(mapper.Map<UsuarioDto>(usuario));
    }

    private async Task AvisarAdminsAsync(Usuario nuevo, CancellationToken ct)
    {
        var admins = await usuarioRepository.GetByRolAsync(RolUsuario.Admin, ct);

        foreach (var admin in admins)
        {
            await EnviarSinRompirAsync(
                admin.Email,
                "Hay una cuenta esperando aprobación",
                $"""
                {nuevo.NombreCompleto} ({nuevo.Email}) se registró en Teleradiología
                y espera que le habilites el acceso.

                Entrá a la sección de Usuarios para aprobar o rechazar la solicitud.
                """,
                ct);
        }
    }

    private async Task EnviarSinRompirAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct)
    {
        try
        {
            await emailSender.EnviarAsync(destinatario, asunto, cuerpo, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo notificar a {Destinatario}: {Asunto}", destinatario, asunto);
        }
    }

    private static string MensajeDeEstado(Usuario usuario) => usuario.EstadoAcceso switch
    {
        EstadoAcceso.Pendiente => "Tu cuenta todavía espera la aprobación de un administrador.",
        EstadoAcceso.Rechazado => "Tu solicitud de acceso fue rechazada.",
        EstadoAcceso.Suspendido => "Tu acceso está suspendido. Contactá a un administrador.",
        _ => "Tu cuenta no está habilitada.",
    };
}
