# SDDP Backend Dockerfile
# Multi-stage build for .NET 10 API

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.300
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac

# Build stage
FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /workspace/src/backend
COPY sddp.config.json /workspace/sddp.config.json

# Copy project files for restore (cache-friendly layer)
COPY src/backend/Sddp.Abstractions/*.csproj ./Sddp.Abstractions/
COPY src/backend/Sddp.Domain/*.csproj ./Sddp.Domain/
COPY src/backend/Sddp.Infrastructure/*.csproj ./Sddp.Infrastructure/
COPY src/backend/Sddp.Application/*.csproj ./Sddp.Application/
COPY src/backend/Sddp.Api/*.csproj ./Sddp.Api/

# Restore dependencies (this layer stays cached unless csproj files change)
RUN dotnet restore Sddp.Api/Sddp.Api.csproj

# Copy all source code
COPY src/backend/ .

# Build and publish
RUN dotnet publish Sddp.Api/Sddp.Api.csproj -c Release -o /app

# Runtime stage (self-contained: no prebuilt local image required)
FROM ${DOTNET_ASPNET_IMAGE} AS runtime

# Runtime native deps needed by Npgsql (GSSAPI)
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 git ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5001

# Security: run as non-root user
RUN useradd --no-create-home --shell /bin/false appuser

# Copy published output
COPY --from=build --chown=appuser /app .

# Writable runtime output (git clones, materialized artifact descriptors). /app itself is
# root-owned on purpose, so a relative default like "data/artifact-descriptors" would land
# in an unwritable directory and every write would fail at runtime.
RUN mkdir -p /app/data && chown appuser /app/data

USER appuser

# Set entrypoint
ENTRYPOINT ["dotnet", "Sddp.Api.dll"]
