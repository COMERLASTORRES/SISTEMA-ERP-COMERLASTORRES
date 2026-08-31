# ============================================================
# STAGE 1: BUILD (SDK completo)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solution y proyectos para cache de restore
COPY SistemaERP.sln .
COPY src/SistemaERP.Api/SistemaERP.Api.csproj src/SistemaERP.Api/
COPY src/SistemaERP.Application/SistemaERP.Application.csproj src/SistemaERP.Application/
COPY src/SistemaERP.Domain/SistemaERP.Domain.csproj src/SistemaERP.Domain/
COPY src/SistemaERP.Infrastructure/SistemaERP.Infrastructure.csproj src/SistemaERP.Infrastructure/

# Restore (cache layer)
RUN dotnet restore SistemaERP.sln

# Copiar todo y compilar
COPY . .
RUN dotnet publish src/SistemaERP.Api/SistemaERP.Api.csproj -c Release -o /app/publish --no-restore

# ============================================================
# STAGE 2: RUNTIME (solo runtime, ~100MB vs ~1.5GB SDK)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Usuario no-root (seguridad)
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copiar artefactos publicados
COPY --from=build /app/publish .

# Puerto expuesto (Railway inyecta PORT, pero documentamos 8080)
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "SistemaERP.Api.dll"]