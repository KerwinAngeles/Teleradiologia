FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Los .csproj primero: capa cacheable, restore solo se rehace si cambian dependencias.
COPY src/Teleradiologia.Domain/Teleradiologia.Domain.csproj src/Teleradiologia.Domain/
COPY src/Teleradiologia.Application/Teleradiologia.Application.csproj src/Teleradiologia.Application/
COPY src/Teleradiologia.Infrastructure/Teleradiologia.Infrastructure.csproj src/Teleradiologia.Infrastructure/
COPY src/Teleradiologia.Infrastructure.Identity/Teleradiologia.Infrastructure.Identity.csproj src/Teleradiologia.Infrastructure.Identity/
COPY src/Teleradiologia.Workers/Teleradiologia.Workers.csproj src/Teleradiologia.Workers/
COPY src/Teleradiologia.Api/Teleradiologia.Api.csproj src/Teleradiologia.Api/
RUN dotnet restore src/Teleradiologia.Api/Teleradiologia.Api.csproj

COPY src/Teleradiologia.Domain/ src/Teleradiologia.Domain/
COPY src/Teleradiologia.Application/ src/Teleradiologia.Application/
COPY src/Teleradiologia.Infrastructure/ src/Teleradiologia.Infrastructure/
COPY src/Teleradiologia.Infrastructure.Identity/ src/Teleradiologia.Infrastructure.Identity/
COPY src/Teleradiologia.Workers/ src/Teleradiologia.Workers/
COPY src/Teleradiologia.Api/ src/Teleradiologia.Api/
RUN dotnet publish src/Teleradiologia.Api/Teleradiologia.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# La imagen de runtime no trae curl, y el HEALTHCHECK del compose de producción lo necesita.
RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Teleradiologia.Api.dll"]
