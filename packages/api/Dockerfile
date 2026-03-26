# Build context: run from repo root with:
#   docker build -f packages/api/Dockerfile .
#
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all source files
COPY packages/api/ packages/api/
COPY packages/worker/Tungsten.Worker/ packages/worker/Tungsten.Worker/

# Restore, build, publish
WORKDIR /src/packages/api
RUN dotnet restore src/Tungsten.Api/Tungsten.Api.csproj
RUN dotnet publish src/Tungsten.Api/Tungsten.Api.csproj -c Release -o /app --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# QuestPDF needs libfontconfig for PDF generation
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Run as non-root
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Tungsten.Api.dll"]
