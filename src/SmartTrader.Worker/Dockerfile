FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.202 AS build
WORKDIR /src
COPY ["SmartTrader.sln", "."]
COPY ["src/SmartTrader.Domain/SmartTrader.Domain.csproj", "src/SmartTrader.Domain/"]
COPY ["src/SmartTrader.Application/SmartTrader.Application.csproj", "src/SmartTrader.Application/"]
COPY ["src/SmartTrader.Infrastructure/SmartTrader.Infrastructure.csproj", "src/SmartTrader.Infrastructure/"]
COPY ["src/SmartTrader.Worker/SmartTrader.Worker.csproj", "src/SmartTrader.Worker/"]
RUN dotnet restore "src/SmartTrader.Worker/SmartTrader.Worker.csproj"
COPY . .
WORKDIR "/src/src/SmartTrader.Worker"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartTrader.Worker.dll"]


