# syntax=docker/dockerfile:1
# Build from the repo root:  docker build -f FinanceApp.Web/Dockerfile -t financeapp-web .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinanceApp.slnx .
COPY FinanceApp.Domain/*.csproj FinanceApp.Domain/
COPY FinanceApp.Application/*.csproj FinanceApp.Application/
COPY FinanceApp.Infrastructure/*.csproj FinanceApp.Infrastructure/
COPY FinanceApp.Localization/*.csproj FinanceApp.Localization/
COPY FinanceApp.Web/*.csproj FinanceApp.Web/
RUN dotnet restore FinanceApp.Web/FinanceApp.Web.csproj

COPY FinanceApp.Domain/ FinanceApp.Domain/
COPY FinanceApp.Application/ FinanceApp.Application/
COPY FinanceApp.Infrastructure/ FinanceApp.Infrastructure/
COPY FinanceApp.Localization/ FinanceApp.Localization/
COPY FinanceApp.Web/ FinanceApp.Web/
# See FinanceApp.API/Dockerfile for why Shared/ is copied alongside the app.
COPY Shared/ Shared/

RUN dotnet publish FinanceApp.Web/FinanceApp.Web.csproj \
    --no-restore -c Release -o /app/publish

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

# Uploads volume — mounted at deploy time, see docker-compose.yml.
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot/uploads

USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinanceApp.Web.dll"]
