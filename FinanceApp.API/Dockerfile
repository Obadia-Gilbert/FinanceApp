# syntax=docker/dockerfile:1
# Build from the repo root:  docker build -f FinanceApp.API/Dockerfile -t financeapp-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` layer-caches independently of source changes.
COPY FinanceApp.slnx .
COPY FinanceApp.Domain/*.csproj FinanceApp.Domain/
COPY FinanceApp.Application/*.csproj FinanceApp.Application/
COPY FinanceApp.Infrastructure/*.csproj FinanceApp.Infrastructure/
COPY FinanceApp.Localization/*.csproj FinanceApp.Localization/
COPY FinanceApp.API/*.csproj FinanceApp.API/
RUN dotnet restore FinanceApp.API/FinanceApp.API.csproj

COPY FinanceApp.Domain/ FinanceApp.Domain/
COPY FinanceApp.Application/ FinanceApp.Application/
COPY FinanceApp.Infrastructure/ FinanceApp.Infrastructure/
COPY FinanceApp.Localization/ FinanceApp.Localization/
COPY FinanceApp.API/ FinanceApp.API/
# Shared/appsettings.shared.json is loaded via a `../Shared/` relative path at runtime
# (see FinanceApp.API/Program.cs) — copy it alongside the published output so that path
# resolves the same way it does in local dev. See GOING_LIVE.md for the env-var alternative.
COPY Shared/ Shared/

RUN dotnet publish FinanceApp.API/FinanceApp.API.csproj \
    --no-restore -c Release -o /app/publish

# Migrator: same source, SDK-equipped, carries `dotnet ef`. Used by CD to run
# `dotnet ef database update` as its own step BEFORE the app containers restart —
# see GOING_LIVE.md on why EnsureCreatedAsync at app startup is not an acceptable
# substitute for this. Not part of the runtime image; pushed as its own tag.
FROM build AS migrator
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", \
            "--project", "FinanceApp.Infrastructure/FinanceApp.Infrastructure.csproj", \
            "--startup-project", "FinanceApp.API/FinanceApp.API.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is needed for the container healthcheck (compose/CD both probe /health)
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

RUN addgroup --system --gid 1000 app \
 && adduser --system --uid 1000 --ingroup app --home /app app

COPY --from=build --chown=app:app /app/publish .
COPY --from=build --chown=app:app /src/Shared /Shared

USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinanceApp.API.dll"]
