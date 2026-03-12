# SDDP Backend Dockerfile
# Multi-stage build for .NET 10 API

ARG DOTNET_VERSION=10.0

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy project files for restore (cache-friendly layer)
COPY Sddp.Abstractions/*.csproj ./Sddp.Abstractions/
COPY Sddp.Domain/*.csproj ./Sddp.Domain/
COPY Sddp.Infrastructure/*.csproj ./Sddp.Infrastructure/
COPY Sddp.Application/*.csproj ./Sddp.Application/
COPY Sddp.Api/*.csproj ./Sddp.Api/

# Restore dependencies (this layer stays cached unless csproj files change)
RUN dotnet restore Sddp.Api/Sddp.Api.csproj

# Copy all source code
COPY . .

# Build and publish
RUN dotnet publish Sddp.Api/Sddp.Api.csproj -c Release -o /app

# Runtime stage (self-contained: no prebuilt local image required)
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

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

USER appuser

# Set entrypoint
ENTRYPOINT ["dotnet", "Sddp.Api.dll"]
