FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartTrader.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/SmartTrader.Domain/SmartTrader.Domain.csproj", "src/SmartTrader.Domain/"]
COPY ["src/SmartTrader.Application/SmartTrader.Application.csproj", "src/SmartTrader.Application/"]
COPY ["src/SmartTrader.Infrastructure/SmartTrader.Infrastructure.csproj", "src/SmartTrader.Infrastructure/"]
COPY ["src/SmartTrader.Presentation.AdminApi/SmartTrader.Presentation.AdminApi.csproj", "src/SmartTrader.Presentation.AdminApi/"]
COPY ["src/SmartTrader.Worker/SmartTrader.Worker.csproj", "src/SmartTrader.Worker/"]
COPY ["tests/SmartTrader.IntegrationTests/SmartTrader.IntegrationTests.csproj", "tests/SmartTrader.IntegrationTests/"]
RUN dotnet restore "SmartTrader.sln"
COPY . .
WORKDIR "/src/src/SmartTrader.Presentation.AdminApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false
WORKDIR /src
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef
RUN mkdir -p /app/bundles
RUN dotnet ef migrations bundle --project src/SmartTrader.Infrastructure/SmartTrader.Infrastructure.csproj --startup-project src/SmartTrader.Presentation.AdminApi/SmartTrader.Presentation.AdminApi.csproj --configuration Release --output /app/bundles/migrate --force --context AppDbContext

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/bundles/migrate ./migrate
COPY src/SmartTrader.Presentation.AdminApi/docker-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh ./migrate
ENTRYPOINT ["./entrypoint.sh"]

