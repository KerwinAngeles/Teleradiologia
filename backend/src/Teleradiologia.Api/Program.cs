using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Teleradiologia.Api.Authentication;
using Teleradiologia.Api.ExceptionHandling;
using Teleradiologia.Api.Realtime;
using Teleradiologia.Application;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Infrastructure;
using Teleradiologia.Infrastructure.Identity;
using Teleradiologia.Workers;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// SignalR no hereda las JsonOptions de MVC: sin esto los enums viajan como números y el
// frontend recibe tipo: 1 en vez de "EstudioUrgente".
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, UsuarioIdProvider>();
builder.Services.AddScoped<INotificadorTiempoReal, NotificadorSignalR>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddWorkers(builder.Configuration);

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR abre el WebSocket con credenciales; sin esto el handshake falla.
            .AllowCredentials();
    });
});

var app = builder.Build();

// Antes que todo: sin esto la IP auditada es la de nginx, no la del usuario.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 2,
};
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaders);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// El proxy de Vite habla HTTP plano; con la redirección activa no se puede loguear.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificacionesHub>(NotificacionesHub.Ruta);

await app.Services.VerificarBaseAsync();

app.Run();
