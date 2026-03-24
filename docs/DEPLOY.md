# Deploying Squad-Commerce to Azure Container Apps

This guide walks you through deploying Squad-Commerce to Azure Container Apps using the Azure Developer CLI (`azd`).

## Table of Contents

- [Prerequisites](#prerequisites)
- [What Gets Deployed](#what-gets-deployed)
- [Deployment Steps](#deployment-steps)
- [Configuration](#configuration)
- [Viewing the Application](#viewing-the-application)
- [Monitoring and Observability](#monitoring-and-observability)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)
- [Cleanup](#cleanup)

## Prerequisites

Before deploying, ensure you have:

1. **Azure Subscription**: An active Azure subscription with contributor access
2. **Azure CLI**: Install from https://aka.ms/azure-cli
3. **Azure Developer CLI (azd)**: Install from https://aka.ms/azd-install
4. **Docker Desktop**: Required for building container images (https://www.docker.com/products/docker-desktop/)
5. **.NET 10 SDK**: Required for building the application (https://dotnet.microsoft.com/download)

Verify installations:
```bash
az --version
azd version
docker --version
dotnet --version
```

## What Gets Deployed

When you run `azd up`, the following Azure resources are created:

### Infrastructure Resources

- **Resource Group**: `rg-squad-commerce` (or your custom environment name)
- **Container Apps Environment**: Serverless container hosting environment with built-in Aspire Dashboard
- **Azure Container Registry (ACR)**: Private registry for container images
- **Log Analytics Workspace**: Centralized logging and monitoring
- **Managed Identity**: Azure-managed identity for secure access between resources

### Application Services

1. **API Container App** (`api`)
   - ASP.NET Core Web API with SignalR
   - Hosts Microsoft Agent Framework (MAF) agents
   - Exposes AG-UI SSE streaming endpoint
   - External ingress on HTTPS
   - Port: 8080

2. **Web Container App** (`web`)
   - Blazor Server application with A2UI components
   - SignalR client for real-time updates
   - External ingress on HTTPS
   - Port: 8080
   - Automatically configured to communicate with `api` service

### Aspire Dashboard

The Container Apps Environment includes the built-in **Aspire Dashboard** for:
- OpenTelemetry traces (distributed tracing)
- Custom metrics (agents, MCP, A2A, AG-UI)
- Structured logs
- Resource health

## Deployment Steps

### Step 1: Authenticate with Azure

```bash
az login
```

Select your Azure subscription:
```bash
az account set --subscription "<your-subscription-id>"
```

### Step 2: Initialize Azure Developer CLI (Already Done)

The project is pre-configured with `azure.yaml` and generated Bicep infrastructure. If you need to regenerate:

```bash
azd init --from-code --environment squad-commerce
```

### Step 3: Deploy to Azure

Run the deployment command:

```bash
azd up
```

This single command will:
1. Prompt for Azure region (e.g., `eastus`, `westus2`, `canadacentral`)
2. Provision all infrastructure using Bicep templates
3. Build Docker images for `api` and `web`
4. Push images to Azure Container Registry
5. Deploy Container Apps
6. Configure service discovery and networking

**Estimated deployment time:** 5-10 minutes

### Step 4: View Deployment Outputs

After successful deployment, `azd` will output:

- API endpoint URL (e.g., `https://api--<unique-id>.<region>.azurecontainerapps.io`)
- Web endpoint URL (e.g., `https://web--<unique-id>.<region>.azurecontainerapps.io`)
- Aspire Dashboard URL (for observability)

## Configuration

### Environment Variables

The deployment automatically configures:

**API Container App:**
- `ASPNETCORE_URLS=http://+:8080`
- `OTEL_EXPORTER_OTLP_ENDPOINT` (auto-configured for Aspire Dashboard)
- `AZURE_CLIENT_ID` (managed identity)
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` (for ingress)

**Web Container App:**
- `ASPNETCORE_URLS=http://+:8080`
- `services__api__https__0` (service discovery URL for API)
- `API_HTTPS` (direct API URL)
- `AZURE_CLIENT_ID` (managed identity)

### Custom Configuration

To add custom configuration:

1. Edit the manifest templates:
   - `src/SquadCommerce.AppHost/infra/api.tmpl.yaml`
   - `src/SquadCommerce.AppHost/infra/web.tmpl.yaml`

2. Add environment variables under `properties.template.containers[].env`

3. Redeploy:
   ```bash
   azd deploy
   ```

### Database Configuration

**Important:** SQLite is used for demo purposes. In the ephemeral container environment:
- Database files are stored in-memory or in ephemeral storage
- **Data is lost on container restart**
- For production, migrate to Azure SQL Database or Azure Cosmos DB

To add Azure SQL:
1. Add SQL resource to `infra/resources.bicep`
2. Update connection string in `api.tmpl.yaml`
3. Update `Program.cs` to use SQL Server provider

## Viewing the Application

### Web Application

Navigate to the Web endpoint URL (printed after `azd up`):
```
https://web--<unique-id>.<region>.azurecontainerapps.io
```

### API Endpoints

- Health check: `https://api--<unique-id>.<region>.azurecontainerapps.io/health`
- AG-UI stream: `https://api--<unique-id>.<region>.azurecontainerapps.io/api/agui?sessionId=<session>`
- Agent list: `https://api--<unique-id>.<region>.azurecontainerapps.io/api/agents`

## Monitoring and Observability

### Aspire Dashboard

Access the built-in Aspire Dashboard:

1. Get the dashboard URL:
   ```bash
   azd show
   ```
   Look for "Aspire Dashboard URL" in the output.

2. View:
   - **Traces**: Distributed tracing for agent invocations, MCP tool calls, A2A handshakes
   - **Metrics**: Custom metrics for agents, tools, protocols, pricing decisions
   - **Logs**: Structured JSON logs with correlation context

### Azure Portal Monitoring

1. Navigate to the Azure Portal: https://portal.azure.com
2. Go to your Resource Group: `rg-squad-commerce`
3. Select the Container App (`api` or `web`)
4. View:
   - **Metrics**: CPU, Memory, HTTP requests, response times
   - **Log stream**: Real-time console logs
   - **Revisions**: Deployment history and health
   - **Diagnostics**: Built-in troubleshooting tools

### Log Analytics Queries

Run KQL queries in Log Analytics Workspace:

```kusto
// View all API logs
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "api"
| order by TimeGenerated desc
| take 100

// View failed requests
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "api"
| where Log_s contains "error" or Log_s contains "exception"
| order by TimeGenerated desc

// View agent invocations
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "api"
| where Log_s contains "ChiefSoftwareArchitect" or Log_s contains "InventoryAgent"
| order by TimeGenerated desc
```

## CI/CD Integration

### GitHub Actions

Configure automated deployment on push:

```bash
azd pipeline config
```

Select **GitHub** as the provider. This will:
1. Create a service principal in Azure
2. Store secrets in GitHub repository
3. Generate `.github/workflows/azure-dev.yml`
4. Enable push-to-deploy on `main` branch

### Azure DevOps

Configure Azure Pipelines:

```bash
azd pipeline config
```

Select **Azure DevOps** as the provider.

### Manual Deployment

To deploy manually after code changes:

```bash
# Deploy both services
azd deploy

# Deploy only API
azd deploy api

# Deploy only Web
azd deploy web
```

## Troubleshooting

### Issue: Container fails to start

**Solution:**
1. Check container logs:
   ```bash
   azd show
   ```
   Click on the Azure Portal link for the failing service.

2. Navigate to **Log stream** to view real-time logs

3. Check **Revisions** → Click failing revision → **Status details**

### Issue: Service discovery not working

**Symptom:** Web can't connect to API

**Solution:**
- Verify environment variables in Web manifest:
  ```bash
  az containerapp show --name web --resource-group rg-squad-commerce --query "properties.template.containers[0].env"
  ```
- Look for `services__api__https__0` pointing to API URL

### Issue: Database data lost on restart

**Expected behavior:** SQLite is ephemeral in containers.

**Solution:** Migrate to Azure SQL or Cosmos DB for persistent storage.

### Issue: Build fails in Docker

**Solution:**
1. Test Docker build locally:
   ```bash
   cd src/SquadCommerce.Api
   docker build -t squadcommerce-api -f Dockerfile ../..
   ```

2. Check Dockerfile paths match project structure

### Issue: OpenTelemetry traces not appearing

**Solution:**
- Verify `OTEL_EXPORTER_OTLP_ENDPOINT` is set in container environment
- Check Aspire Dashboard URL is accessible
- Ensure `SquadCommerceMetrics` singleton is registered in DI

### Getting Help

View deployment details:
```bash
azd show
```

View environment variables:
```bash
azd env get-values
```

View logs:
```bash
azd logs
```

## Cleanup

To delete all Azure resources and stop incurring charges:

```bash
azd down
```

Confirm the deletion when prompted. This will:
- Delete the resource group and all resources
- Remove the Container Apps Environment
- Delete the Container Registry and images
- Clean up Log Analytics Workspace

**Cost estimate:** Running 2 Container Apps on consumption plan costs approximately $5-15/month depending on usage.

## Additional Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [OpenTelemetry on Azure](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)

---

**Next Steps:**
1. Run `azd up` to deploy
2. Test the Web application at the deployed URL
3. Trigger competitor price analysis scenario
4. View traces in Aspire Dashboard
5. Set up CI/CD with `azd pipeline config`
