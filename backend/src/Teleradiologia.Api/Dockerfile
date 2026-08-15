FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos los .csproj primero (capa cacheable) — restore solo se re-ejecuta si cambian dependencias,
# no en cada cambio de código.
COPY src/Teleradiologia.Domain/Teleradiologia.Domain.csproj src/Teleradiologia.Domain/
COPY src/Teleradiologia.Application/Teleradiologia.Application.csproj src/Teleradiologia.Application/
COPY src/Teleradiologia.Infrastructure/Teleradiologia.Infrastructure.csproj src/Teleradiologia.Infrastructure/
COPY src/Teleradiologia.Api/Teleradiologia.Api.csproj src/Teleradiologia.Api/
RUN dotnet restore src/Teleradiologia.Api/Teleradiologia.Api.csproj

COPY src/Teleradiologia.Domain/ src/Teleradiologia.Domain/
COPY src/Teleradiologia.Application/ src/Teleradiologia.Application/
COPY src/Teleradiologia.Infrastructure/ src/Teleradiologia.Infrastructure/
COPY src/Teleradiologia.Api/ src/Teleradiologia.Api/
RUN dotnet publish src/Teleradiologia.Api/Teleradiologia.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# La imagen de runtime de ASP.NET no trae curl ni wget, así que sin esto el HEALTHCHECK del
# compose de producción no tiene con qué pegarle a /api/health.
RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Teleradiologia.Api.dll"]
