# =============================================================================
#  Интернет-магазин «Садовод» — многоэтапная сборка образа приложения (.NET 10)
#  Этап build  — на SDK-образе: restore + publish.
#  Этап final  — на ASP.NET-runtime-образе: только опубликованные артефакты,
#                без исходного кода и инструментов сборки.
# =============================================================================

# ---- Этап сборки -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — чтобы слой restore кэшировался независимо от кода.
COPY ["Sadovod.csproj", "./"]
RUN dotnet restore "Sadovod.csproj"

# Остальной исходный код и публикация в Release.
COPY . .
RUN dotnet publish "Sadovod.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Финальный образ -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Kestrel слушает 8080 внутри контейнера (по умолчанию для aspnet-образов .NET 8+).
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Sadovod.dll"]
