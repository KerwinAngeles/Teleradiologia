using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Teleradiologia.Application.Estudios;
using Teleradiologia.Application.Informes;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Application.Services;

namespace Teleradiologia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        services.AddScoped<IEstudioService, EstudioService>();
        services.AddScoped<IInformeService, InformeService>();
        services.AddScoped<IResumenActividadService, ResumenActividadService>();
        services.AddScoped<IHospitalService, HospitalService>();
        services.AddScoped<IEventoService, EventoService>();
        services.AddScoped<INotificacionService, NotificacionService>();
        services.AddScoped<IPlantillaService, PlantillaService>();

        return services;
    }
}
