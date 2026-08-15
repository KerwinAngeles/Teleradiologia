using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Teleradiologia.Workers.BackgroundServices;
using Teleradiologia.Workers.Options;

namespace Teleradiologia.Workers;

public static class ServiceRegistration
{
    public static IServiceCollection AddWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ResumenActividadOptions>(configuration.GetSection(ResumenActividadOptions.SectionName));

        services.AddHostedService<ResumenActividadBackgroundService>();

        return services;
    }
}
