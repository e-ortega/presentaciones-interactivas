# syntax=docker/dockerfile:1

# 1. Build the React app.
FROM node:24-alpine AS web
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
# --ignore-scripts: the "prepare" script installs git hooks, which an image build does not need.
RUN npm ci --ignore-scripts
COPY src/web/ ./
RUN npm run build

# 2. Publish the server with the React build as its static files.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Server/Server.csproj src/Server/
RUN dotnet restore src/Server/Server.csproj
COPY src/Server/ src/Server/
COPY --from=web /web/dist/ src/Server/wwwroot/
RUN dotnet publish src/Server/Server.csproj -c Release -o /app --no-restore

# 3. Runtime image, with what the importer needs:
#    - LibreOffice Impress converts the PPTX to PDF; poppler-utils (pdftoppm) turns pages into PNG.
#    - Fonts metric-compatible with the Office ones, so text keeps its place:
#      Carlito for Calibri, Caladea for Cambria, Liberation for Arial and Times New Roman.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update  && apt-get install -y --no-install-recommends       libreoffice-impress poppler-utils       fonts-crosextra-carlito fonts-crosextra-caladea fonts-liberation fonts-dejavu-core  && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
RUN mkdir /data && chown $APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080     Storage__LocalRoot=/data
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Server.dll"]
