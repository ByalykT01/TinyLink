FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl\
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restore
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY TinyLink.Api/TinyLink.Api.csproj TinyLink.Api/
RUN dotnet restore TinyLink.Api/TinyLink.Api.csproj

FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY TinyLink.Api/ TinyLInk.Api/
RUN dotnet build TinyLink.Api/TinyLink.Api.csproj -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TinyLink.Api/TinyLink.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-build \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TinyLink.Api.dll"]

