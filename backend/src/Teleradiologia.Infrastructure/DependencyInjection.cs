using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Application.Interfaces.Repositories;
using Teleradiologia.Infrastructure.Email;
using Teleradiologia.Infrastructure.Health;
using Teleradiologia.Infrastructure.Orthanc;
using Teleradiologia.Infrastructure.Persistence;
using Teleradiologia.Infrastructure.Repositories;

namespace Teleradiologia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IEstudioRepository, EstudioRepository>();
        services.AddScoped<IInformeRepository, InformeRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IResumenActividadRepository, ResumenActividadRepository>();

        services.Configure<OrthancOptions>(configuration.GetSection(OrthancOptions.SectionName));
        services.AddHttpClient<IOrthancClient, OrthancClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OrthancOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

            var credenciales = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credenciales);
        });

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
