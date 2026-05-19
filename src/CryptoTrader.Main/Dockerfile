FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION=1.0.0
WORKDIR /src

COPY *.sln ./
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore

COPY . .
RUN dotnet publish "src/CryptoTrader.Main/CryptoTrader.Main.csproj" -c Release -o /app /p:Version=$APP_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV TZ=Europe/Kiev
ENTRYPOINT ["dotnet", "CryptoTrader.Main.dll"]
