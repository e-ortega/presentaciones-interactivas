# syntax=docker/dockerfile:1

# 1. Build the React app.
FROM node:24-alpine AS web
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
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

# 3. Runtime image. LibreOffice, poppler-utils and the fonts are added in M1.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
RUN mkdir /data && chown $APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Storage__LocalRoot=/data
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Server.dll"]
