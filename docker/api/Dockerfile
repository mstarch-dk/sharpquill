FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Native dependency required by SkiaSharp
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Copy project files first for layer caching
COPY src/backend/*.sln ./
COPY src/backend/api/*.csproj ./api/
COPY src/backend/core/*.csproj ./core/
COPY src/backend/application/*.csproj ./application/
COPY src/backend/migrations/*.csproj ./migrations/
COPY src/backend/api.Tests/*.csproj ./api.Tests/
COPY src/backend/db-init/*.csproj ./db-init/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/backend/ ./

# Set working directory to API project
WORKDIR /app/api

# Expose port
EXPOSE 5000

# Run with hot reload
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
