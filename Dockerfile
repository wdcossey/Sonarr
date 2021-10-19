FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# Manually added for the moment...
COPY ["Logo/*", "Logo/"]
COPY ["src/Directory.Build.props", "src/"]
COPY ["src/LogentriesNLog/LogentriesNLog.csproj", "src/LogentriesNLog/"]
COPY ["src/LogentriesCore/LogentriesCore.csproj", "src/LogentriesCore/"]
COPY ["src/Marr.Data/Marr.Data.csproj", "src/Marr.Data/"]
COPY ["src/MonoTorrent/MonoTorrent.csproj", "src/MonoTorrent/"]
COPY ["src/NzbDrone.Core/Sonarr.Core.csproj", "src/NzbDrone.Core/"]
COPY ["src/NzbDrone.Common/Sonarr.Common.csproj", "src/NzbDrone.Common/"]
COPY ["src/NzbDrone.Host/Sonarr.Host.csproj", "src/NzbDrone.Host/"]
COPY ["src/Sonarr.Server/Sonarr.Server.csproj", "src/Sonarr.Server/"]

RUN dotnet restore "src/Sonarr.Server/Sonarr.Server.csproj"
COPY . .
WORKDIR "/src/src"
# RUN dotnet --info -v d
RUN dotnet build "NzbDrone.Host/Sonarr.Host.csproj" -c Release -o /app/build -v n
RUN dotnet build "Sonarr.Server/Sonarr.Server.csproj" -c Release -o /app/build -v n

FROM build AS publish
RUN dotnet publish "NzbDrone.Host/Sonarr.Host.csproj" -c Release -o /app/publish
RUN dotnet publish "Sonarr.Server/Sonarr.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sonarr.Server.dll"]
