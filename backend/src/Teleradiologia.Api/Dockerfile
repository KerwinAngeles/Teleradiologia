FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Teleradiologia.Api/Teleradiologia.Api.csproj src/Teleradiologia.Api/
RUN dotnet restore src/Teleradiologia.Api/Teleradiologia.Api.csproj

COPY src/Teleradiologia.Api/ src/Teleradiologia.Api/
RUN dotnet publish src/Teleradiologia.Api/Teleradiologia.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Teleradiologia.Api.dll"]
