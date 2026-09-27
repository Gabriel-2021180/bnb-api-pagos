# syntax=docker/dockerfile:1

# El SDK corre en la arquitectura del host y compila para la de destino (amd64 o arm64).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restauración en una capa aparte: solo se repite si cambian los .csproj o las versiones de paquetes.
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ApiPagos.Domain/ApiPagos.Domain.csproj src/ApiPagos.Domain/
COPY src/ApiPagos.Application/ApiPagos.Application.csproj src/ApiPagos.Application/
COPY src/ApiPagos.Infrastructure/ApiPagos.Infrastructure.csproj src/ApiPagos.Infrastructure/
COPY src/ApiPagos.Api/ApiPagos.Api.csproj src/ApiPagos.Api/
COPY tests/ApiPagos.UnitTests/ApiPagos.UnitTests.csproj tests/ApiPagos.UnitTests/
RUN dotnet restore src/ApiPagos.Api/ApiPagos.Api.csproj -a $TARGETARCH \
 && dotnet restore tests/ApiPagos.UnitTests/ApiPagos.UnitTests.csproj

COPY src/ src/
COPY tests/ tests/

# Opcional: docker build --target test .
FROM build AS test
RUN dotnet test tests/ApiPagos.UnitTests/ApiPagos.UnitTests.csproj -c Release --no-restore

FROM build AS publish
# Publicar para una sola arquitectura evita copiar librerías nativas de Windows, macOS, etc.
# librdkafka (Kafka): en x64 se deja solo la variante centos8, enlazada estáticamente (la genérica
# necesita libsasl2, que la imagen final no trae). En arm64 existe una sola variante, ya estática.
RUN dotnet publish src/ApiPagos.Api/ApiPagos.Api.csproj -c Release -a $TARGETARCH --self-contained false \
        -o /app/publish --no-restore /p:UseAppHost=false \
 && rm -f /app/publish/alpine-librdkafka.so \
 && if [ -f /app/publish/centos8-librdkafka.so ]; then rm -f /app/publish/librdkafka.so; fi

# Imagen "chiseled": Ubuntu mínimo sin shell ni gestor de paquetes, usuario no root.
# La variante "extra" incluye ICU, que necesita Microsoft.Data.SqlClient.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ApiPagos.Api.dll"]
