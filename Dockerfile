FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
EXPOSE 8080

# ---------- Frontend ----------
# Compiled with no VITE_* values baked in, so the SPA defaults to same-origin
# API calls (window.location.origin). For a split-origin deploy, override at
# image build time, e.g.:
#   docker build --build-arg VITE_API_BASE_URL=https://api.example.com .
FROM node:26-alpine AS frontend
WORKDIR /web
ARG VITE_API_BASE_URL=""
ARG VITE_REDIRECT_BASE_URL=""
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restore
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY TinyLink.Api/TinyLink.Api.csproj TinyLink.Api/
RUN dotnet restore TinyLink.Api/TinyLink.Api.csproj

FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY TinyLink.Api/ TinyLink.Api/
COPY --from=frontend /web/dist TinyLink.Api/wwwroot
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
