# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files for dependency resolution
COPY ["SquadCommerce.slnx", "./"]
COPY ["src/SquadCommerce.Api/SquadCommerce.Api.csproj", "src/SquadCommerce.Api/"]
COPY ["src/SquadCommerce.ServiceDefaults/SquadCommerce.ServiceDefaults.csproj", "src/SquadCommerce.ServiceDefaults/"]
COPY ["src/SquadCommerce.Contracts/SquadCommerce.Contracts.csproj", "src/SquadCommerce.Contracts/"]
COPY ["src/SquadCommerce.Agents/SquadCommerce.Agents.csproj", "src/SquadCommerce.Agents/"]
COPY ["src/SquadCommerce.Mcp/SquadCommerce.Mcp.csproj", "src/SquadCommerce.Mcp/"]
COPY ["src/SquadCommerce.A2A/SquadCommerce.A2A.csproj", "src/SquadCommerce.A2A/"]

# Restore dependencies
RUN dotnet restore "src/SquadCommerce.Api/SquadCommerce.Api.csproj"

# Copy all source code
COPY ["src/", "src/"]

# Build and publish the application
WORKDIR "/src/src/SquadCommerce.Api"
RUN dotnet build "SquadCommerce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "SquadCommerce.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "SquadCommerce.Api.dll"]
