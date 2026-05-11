FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copiar arquivos de projeto para aproveitar cache de restauração
COPY DesafioHyperativa.sln .
COPY src/DesafioHyperativa.Domain/DesafioHyperativa.Domain.csproj src/DesafioHyperativa.Domain/
COPY src/DesafioHyperativa.Shared/DesafioHyperativa.Shared.csproj src/DesafioHyperativa.Shared/
COPY src/DesafioHyperativa.Application/DesafioHyperativa.Application.csproj src/DesafioHyperativa.Application/
COPY src/DesafioHyperativa.Infrastructure/DesafioHyperativa.Infrastructure.csproj src/DesafioHyperativa.Infrastructure/
COPY src/DesafioHyperativa.API/DesafioHyperativa.API.csproj src/DesafioHyperativa.API/
COPY tests/DesafioHyperativa.UnitTests/DesafioHyperativa.UnitTests.csproj tests/DesafioHyperativa.UnitTests/

RUN dotnet restore && \
    find /tmp -maxdepth 1 \( -name 'MSBuild*' -o -name 'dotnet-diagnostic-*' \) -delete 2>/dev/null || true

# Copiar código-fonte e publicar
COPY . .
RUN dotnet publish src/DesafioHyperativa.API/DesafioHyperativa.API.csproj \
    -c Release \
    -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Usuário não-root para segurança
RUN addgroup --gid 1001 appgroup && \
    adduser --uid 1001 --ingroup appgroup --disabled-password --gecos "" appuser

COPY --from=build /app/publish .
RUN mkdir -p logs && chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

ENTRYPOINT ["dotnet", "DesafioHyperativa.API.dll"]
