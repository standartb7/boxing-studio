# syntax=docker/dockerfile:1
# Build context expected at the repo root: `docker build -f src/Trainer.Api/Dockerfile .`

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY global.json ./
COPY src/Trainer.Core/Trainer.Core.csproj      src/Trainer.Core/
COPY src/Trainer.Data/Trainer.Data.csproj      src/Trainer.Data/
COPY src/Trainer.Contracts/Trainer.Contracts.csproj src/Trainer.Contracts/
COPY src/Trainer.Api/Trainer.Api.csproj        src/Trainer.Api/
RUN dotnet restore src/Trainer.Api/Trainer.Api.csproj

# Copy the rest and publish
COPY src/Trainer.Core/      src/Trainer.Core/
COPY src/Trainer.Data/      src/Trainer.Data/
COPY src/Trainer.Contracts/ src/Trainer.Contracts/
COPY src/Trainer.Api/       src/Trainer.Api/
RUN dotnet publish src/Trainer.Api/Trainer.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Trainer.Api.dll"]
