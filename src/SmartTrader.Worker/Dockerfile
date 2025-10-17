FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartTrader.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/SmartTrader.Domain/SmartTrader.Domain.csproj", "src/SmartTrader.Domain/"]
COPY ["src/SmartTrader.Application/SmartTrader.Application.csproj", "src/SmartTrader.Application/"]
COPY ["src/SmartTrader.Infrastructure/SmartTrader.Infrastructure.csproj", "src/SmartTrader.Infrastructure/"]
COPY ["src/SmartTrader.Worker/SmartTrader.Worker.csproj", "src/SmartTrader.Worker/"]
COPY ["src/SmartTrader.Presentation.AdminApi/SmartTrader.Presentation.AdminApi.csproj", "src/SmartTrader.Presentation.AdminApi/"]
COPY ["tests/SmartTrader.IntegrationTests/SmartTrader.IntegrationTests.csproj", "tests/SmartTrader.IntegrationTests/"]
RUN dotnet restore "SmartTrader.sln"
COPY . .
WORKDIR "/src/src/SmartTrader.Worker"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartTrader.Worker.dll"]



