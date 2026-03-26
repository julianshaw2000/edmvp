# Build context: run from repo root with:
#   docker build -f packages/api/Dockerfile .
#
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching (cache bust: anthropic-sdk)
COPY packages/api/Tungsten.sln packages/api/
COPY packages/api/src/Tungsten.Api/Tungsten.Api.csproj packages/api/src/Tungsten.Api/
COPY packages/api/tests/Tungsten.Api.Tests/Tungsten.Api.Tests.csproj packages/api/tests/Tungsten.Api.Tests/
COPY packages/worker/Tungsten.Worker/Tungsten.Worker.csproj packages/worker/Tungsten.Worker/

# Restore (force fresh restore)
WORKDIR /src/packages/api
RUN dotnet restore src/Tungsten.Api/Tungsten.Api.csproj --force

# Copy all source
WORKDIR /src
COPY packages/api/src/ packages/api/src/
COPY packages/worker/Tungsten.Worker/ packages/worker/Tungsten.Worker/

# Publish
WORKDIR /src/packages/api
RUN dotnet publish src/Tungsten.Api/Tungsten.Api.csproj -c Release -o /app

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
