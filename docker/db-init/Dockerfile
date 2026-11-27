FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/backend/core/core.csproj core/
COPY src/backend/migrations/migrations.csproj migrations/
COPY src/backend/db-init/db-init.csproj db-init/

# Restore dependencies
RUN dotnet restore db-init/db-init.csproj

# Copy source code
COPY src/backend/core/ core/
COPY src/backend/migrations/ migrations/
COPY src/backend/db-init/ db-init/

# Build and publish
RUN dotnet publish db-init/db-init.csproj -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0
# Npgsql probes for GSSAPI at start-up; without the library it logs a harmless but alarming
# "Cannot load library libgssapi_krb5.so.2" error on every run
RUN apt-get update && \
    apt-get install -y --no-install-recommends libgssapi-krb5-2 && \
    rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "db-init.dll"]
