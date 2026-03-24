# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for dependency resolution
COPY ["SquadCommerce.slnx", "./"]
COPY ["src/SquadCommerce.Web/SquadCommerce.Web.csproj", "src/SquadCommerce.Web/"]
COPY ["src/SquadCommerce.Contracts/SquadCommerce.Contracts.csproj", "src/SquadCommerce.Contracts/"]

# Restore dependencies
RUN dotnet restore "src/SquadCommerce.Web/SquadCommerce.Web.csproj"

# Copy all source code
COPY ["src/SquadCommerce.Web/", "src/SquadCommerce.Web/"]
COPY ["src/SquadCommerce.Contracts/", "src/SquadCommerce.Contracts/"]

# Build and publish the application
WORKDIR "/src/src/SquadCommerce.Web"
RUN dotnet build "SquadCommerce.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "SquadCommerce.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables for Azure Container Apps
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "SquadCommerce.Web.dll"]
