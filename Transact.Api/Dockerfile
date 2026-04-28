# syntax=docker/dockerfile:1.7
# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (cached) — copy only csproj files first.
COPY Transact.slnx ./
COPY Transact.Api/Transact.Api.csproj            Transact.Api/
COPY Transact.Application/Transact.Application.csproj   Transact.Application/
COPY Transact.Domain/Transact.Domain.csproj      Transact.Domain/
COPY Transact.Infrastructure/Transact.Infrastructure.csproj Transact.Infrastructure/
RUN dotnet restore Transact.Api/Transact.Api.csproj

# Copy the rest and publish.
COPY Transact.Api/            Transact.Api/
COPY Transact.Application/    Transact.Application/
COPY Transact.Domain/         Transact.Domain/
COPY Transact.Infrastructure/ Transact.Infrastructure/
RUN dotnet publish Transact.Api/Transact.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as non-root.
RUN groupadd --system app && useradd --system --gid app --home /app app \
    && chown -R app:app /app
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true

COPY --from=build --chown=app:app /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "Transact.Api.dll"]
