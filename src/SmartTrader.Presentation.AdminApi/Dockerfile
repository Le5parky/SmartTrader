FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0.202 AS build
WORKDIR /src
COPY ["SmartTrader.sln", "."]
COPY ["src/SmartTrader.Domain/SmartTrader.Domain.csproj", "src/SmartTrader.Domain/"]
COPY ["src/SmartTrader.Application/SmartTrader.Application.csproj", "src/SmartTrader.Application/"]
COPY ["src/SmartTrader.Infrastructure/SmartTrader.Infrastructure.csproj", "src/SmartTrader.Infrastructure/"]
COPY ["src/SmartTrader.Presentation.AdminApi/SmartTrader.Presentation.AdminApi.csproj", "src/SmartTrader.Presentation.AdminApi/"]
RUN dotnet restore "src/SmartTrader.Presentation.AdminApi/SmartTrader.Presentation.AdminApi.csproj"
COPY . .
WORKDIR "/src/src/SmartTrader.Presentation.AdminApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartTrader.Presentation.AdminApi.dll"]


