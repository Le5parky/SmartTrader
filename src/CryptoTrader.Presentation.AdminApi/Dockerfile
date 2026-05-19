FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0.202 AS build
WORKDIR /src
COPY ["CryptoTrader.sln", "."]
COPY ["src/CryptoTrader.Domain/CryptoTrader.Domain.csproj", "src/CryptoTrader.Domain/"]
COPY ["src/CryptoTrader.Application/CryptoTrader.Application.csproj", "src/CryptoTrader.Application/"]
COPY ["src/CryptoTrader.Infrastructure/CryptoTrader.Infrastructure.csproj", "src/CryptoTrader.Infrastructure/"]
COPY ["src/CryptoTrader.Presentation.AdminApi/CryptoTrader.Presentation.AdminApi.csproj", "src/CryptoTrader.Presentation.AdminApi/"]
RUN dotnet restore "src/CryptoTrader.Presentation.AdminApi/CryptoTrader.Presentation.AdminApi.csproj"
COPY . .
WORKDIR "/src/src/CryptoTrader.Presentation.AdminApi"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false
WORKDIR /src
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef
RUN mkdir -p /app/bundles
RUN dotnet ef migrations bundle --project src/CryptoTrader.Infrastructure/CryptoTrader.Infrastructure.csproj --startup-project src/CryptoTrader.Presentation.AdminApi/CryptoTrader.Presentation.AdminApi.csproj --configuration Release --self-contained false --output /app/bundles/migrate --force --context AppDbContext

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/bundles/migrate ./migrate
COPY src/CryptoTrader.Presentation.AdminApi/docker-entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh ./migrate
ENTRYPOINT ["./entrypoint.sh"]
