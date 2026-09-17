# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and configuration files
COPY TaskManagement.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY .editorconfig .

# Copy project files for layer caching
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj src/TaskManagement.Infrastructure/
COPY src/TaskManagement.API/TaskManagement.API.csproj src/TaskManagement.API/
COPY tests/TaskManagement.UnitTests/TaskManagement.UnitTests.csproj tests/TaskManagement.UnitTests/

# Restore dependencies
RUN dotnet restore src/TaskManagement.API/TaskManagement.API.csproj

# Copy source code
COPY src/ src/

# Build and publish the API in Release mode
RUN dotnet publish src/TaskManagement.API/TaskManagement.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Install curl for health check
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000

# Copy published artifacts
COPY --from=build /app/publish .

# Run as non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]

