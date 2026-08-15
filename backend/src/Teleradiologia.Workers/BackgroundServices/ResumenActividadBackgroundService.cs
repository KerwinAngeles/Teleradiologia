using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Workers.Options;

namespace Teleradiologia.Workers.BackgroundServices;

public class ResumenActividadBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ResumenActividadOptions> options,
    ILogger<ResumenActividadBackgroundService> logger) : BackgroundService
{
    private readonly ResumenActividadOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Habilitado)
        {
            logger.LogInformation("El resumen de actividad está deshabilitado.");
            return;
        }

        var intervalo = TimeSpan.FromHours(Math.Max(1, _options.IntervaloHoras));

        if (_options.EjecutarAlArrancar)
        {
            await EjecutarAsync(intervalo, stoppingToken);
        }

        var espera = DemoraHastaElPrimerEnvio();
        if (espera > TimeSpan.Zero)
        {
            logger.LogInformation("Primer resumen de actividad en {Horas:0.0} h.", espera.TotalHours);

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        using var timer = new PeriodicTimer(intervalo);

        do
        {
            await EjecutarAsync(intervalo, stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EjecutarAsync(TimeSpan ventana, CancellationToken ct)
    {
        try
        {
            // Scope propio: el servicio es singleton y los repositorios son scoped.
            using var scope = scopeFactory.CreateScope();
            var resumen = scope.ServiceProvider.GetRequiredService<IResumenActividadService>();

            var hasta = DateTimeOffset.UtcNow;
            var resultado = await resumen.EnviarResumenAsync(hasta - ventana, hasta, ct);

            if (resultado.HasError)
            {
                logger.LogWarning("No se pudo enviar el resumen de actividad: {Error}", resultado.Error);
            }
            else
            {
                logger.LogInformation("Resumen de actividad enviado a {Cantidad} destinatario(s).", resultado.Data);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al enviar el resumen de actividad.");
        }
    }

    private TimeSpan DemoraHastaElPrimerEnvio()
    {
        if (_options.HoraDeEnvio is not { } hora || hora is < 0 or > 23)
        {
            return TimeSpan.Zero;
        }

        var ahora = DateTimeOffset.Now;
        var proximo = new DateTimeOffset(ahora.Year, ahora.Month, ahora.Day, hora, 0, 0, ahora.Offset);

        if (proximo <= ahora)
        {
            proximo = proximo.AddDays(1);
        }

        return proximo - ahora;
    }
}
