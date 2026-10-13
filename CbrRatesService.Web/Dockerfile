# syntax=docker/dockerfile:1
# Pin patch tags — floating `:8.0` can leave corrupt/empty layers on Docker Desktop (Win11).

FROM mcr.microsoft.com/dotnet/sdk:8.0.425 AS build
WORKDIR /src

COPY global.json ./
COPY CbrRatesService.Domain/CbrRatesService.Domain.csproj CbrRatesService.Domain/
COPY CbrRatesService.Infrastructure.Abstractions/CbrRatesService.Infrastructure.Abstractions.csproj CbrRatesService.Infrastructure.Abstractions/
COPY CbrRatesService.Infrastructure.Cbr/CbrRatesService.Infrastructure.Cbr.csproj CbrRatesService.Infrastructure.Cbr/
COPY CbrRatesService.Infrastructure.MemoryCache/CbrRatesService.Infrastructure.MemoryCache.csproj CbrRatesService.Infrastructure.MemoryCache/
COPY CbrRatesService.Infrastructure.Redis/CbrRatesService.Infrastructure.Redis.csproj CbrRatesService.Infrastructure.Redis/
COPY CbrRatesService.Infrastructure/CbrRatesService.Infrastructure.csproj CbrRatesService.Infrastructure/
COPY CbrRatesService.Application/CbrRatesService.Application.csproj CbrRatesService.Application/
COPY CbrRatesService.Web/CbrRatesService.Web.csproj CbrRatesService.Web/

RUN dotnet restore CbrRatesService.Web/CbrRatesService.Web.csproj

COPY CbrRatesService.Domain/ CbrRatesService.Domain/
COPY CbrRatesService.Infrastructure.Abstractions/ CbrRatesService.Infrastructure.Abstractions/
COPY CbrRatesService.Infrastructure.Cbr/ CbrRatesService.Infrastructure.Cbr/
COPY CbrRatesService.Infrastructure.MemoryCache/ CbrRatesService.Infrastructure.MemoryCache/
COPY CbrRatesService.Infrastructure.Redis/ CbrRatesService.Infrastructure.Redis/
COPY CbrRatesService.Infrastructure/ CbrRatesService.Infrastructure/
COPY CbrRatesService.Application/ CbrRatesService.Application/
COPY CbrRatesService.Web/ CbrRatesService.Web/

RUN dotnet publish CbrRatesService.Web/CbrRatesService.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0.22 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CbrRatesService.Web.dll"]
