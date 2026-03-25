# Stage 1: Build React frontend
FROM node:20-alpine AS frontend
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: Build ASP.NET Core backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY KnxMqttBridge.Web.csproj ./
RUN dotnet restore
COPY . .
COPY --from=frontend /app/wwwroot ./wwwroot
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "KnxMqttBridge.Web.dll"]
