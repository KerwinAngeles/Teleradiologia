using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Common;
using Teleradiologia.Application.Dtos.Resumen;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Services;

public class ResumenActividadService(
    IResumenActividadRepository resumenRepository,
    IUsuarioRepository usuarioRepository,
    IEmailSender emailSender,
    ILogger<ResumenActividadService> logger) : IResumenActividadService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    public async Task<BaseResponse<ResumenActividadDto>> ObtenerAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        if (hasta <= desde)
        {
            return BaseResponse<ResumenActividadDto>.Fail("El rango de fechas es inválido.");
        }

        var recibidos = await resumenRepository.ContarEstudiosRecibidosAsync(desde, hasta, ct);
        var firmados = await resumenRepository.ContarInformesFirmadosAsync(desde, hasta, adendas: false, ct);
        var adendas = await resumenRepository.ContarInformesFirmadosAsync(desde, hasta, adendas: true, ct);
        var informados = await resumenRepository.ContarEstudiosPorEstadoAsync(EstadoEstudio.Informado, ct);
        var pendientes = await resumenRepository.ContarEstudiosPorEstadoAsync(EstadoEstudio.Pendiente, ct);
        var enInforme = await resumenRepository.ContarEstudiosPorEstadoAsync(EstadoEstudio.EnInforme, ct);

        var porRadiologo = new List<FirmasPorRadiologoDto>();
        foreach (var fila in await resumenRepository.ContarFirmasPorRadiologoAsync(desde, hasta, ct))
        {
            var radiologo = await usuarioRepository.GetByIdAsync(fila.RadiologoId, ct);
            porRadiologo.Add(new FirmasPorRadiologoDto(radiologo?.NombreCompleto ?? "(usuario eliminado)", fila.Firmados));
        }

        return BaseResponse<ResumenActividadDto>.Success(new ResumenActividadDto(
            desde, hasta, recibidos, firmados, adendas, informados, pendientes, enInforme, porRadiologo));
    }

    public async Task<BaseResponse<int>> EnviarResumenAsync(DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        var resumen = await ObtenerAsync(desde, hasta, ct);
        if (resumen.HasError)
        {
            return BaseResponse<int>.Fail(resumen.Error!, resumen.Code ?? ErrorCode.Invalido);
        }

        // Sin movimiento no se manda nada: un resumen vacío por día entrena a ignorar el aviso.
        if (resumen.Data!.SinActividad)
        {
            logger.LogInformation("Sin actividad entre {Desde} y {Hasta}: no se envía resumen.", desde, hasta);
            return BaseResponse<int>.Success(0);
        }

        var destinatarios = await usuarioRepository.GetByRolAsync(RolUsuario.Admin, ct);
        if (destinatarios.Count == 0)
        {
            return BaseResponse<int>.Fail("No hay administradores activos a quienes enviar el resumen.", ErrorCode.NoEncontrado);
        }

        var asunto = $"Resumen de actividad — {desde.ToString("dd/MM/yyyy", Cultura)}";
        var cuerpo = Redactar(resumen.Data);

        var enviados = 0;
        foreach (var admin in destinatarios)
        {
            try
            {
                await emailSender.EnviarAsync(admin.Email, asunto, cuerpo, ct);
                enviados++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo enviar el resumen a {Email}", admin.Email);
            }
        }

        return BaseResponse<int>.Success(enviados);
    }

    private static string Redactar(ResumenActividadDto r)
    {
        var texto = new StringBuilder();

        texto.AppendLine($"Actividad entre el {r.Desde.ToString("dd/MM/yyyy HH:mm", Cultura)} y el {r.Hasta.ToString("dd/MM/yyyy HH:mm", Cultura)}.");
        texto.AppendLine();
        texto.AppendLine($"  Estudios recibidos ....... {r.EstudiosRecibidos}");
        texto.AppendLine($"  Informes firmados ........ {r.InformesFirmados}");
        texto.AppendLine($"  Adendas firmadas ......... {r.AdendasFirmadas}");
        texto.AppendLine();
        texto.AppendLine("Estado de la cola:");
        texto.AppendLine($"  Pendientes ............... {r.EstudiosPendientes}");
        texto.AppendLine($"  En informe ............... {r.EstudiosEnInforme}");
        texto.AppendLine($"  Informados (total) ....... {r.EstudiosInformados}");

        if (r.PorRadiologo.Count > 0)
        {
            texto.AppendLine();
            texto.AppendLine("Firmas por radiólogo:");

            foreach (var fila in r.PorRadiologo)
            {
                texto.AppendLine($"  {fila.Radiologo} — {fila.Firmados}");
            }
        }

        return texto.ToString();
    }
}
