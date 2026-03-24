# Azure Deployment Checklist

Use this checklist before running `azd up`:

## Prerequisites ✓

- [ ] Azure subscription with contributor access
- [ ] Azure CLI installed (`az --version`)
- [ ] Azure Developer CLI installed (`azd version`)
- [ ] Docker Desktop running (`docker --version`)
- [ ] .NET 10 SDK installed (`dotnet --version`)
- [ ] Authenticated with Azure (`az login`)

## Pre-Deployment Verification

- [ ] Solution builds successfully: `dotnet build SquadCommerce.slnx`
- [ ] All tests pass: `dotnet test`
- [ ] Reviewed `azure.yaml` configuration
- [ ] Reviewed Bicep templates in `infra/` directory
- [ ] Reviewed container manifests in `src/SquadCommerce.AppHost/infra/`

## Deployment

- [ ] Run `azd up` from project root
- [ ] Select Azure region when prompted
- [ ] Wait for provisioning (5-10 minutes)
- [ ] Note deployment outputs (URLs)

## Post-Deployment Verification

- [ ] API health check responds: `<api-url>/health`
- [ ] Web application loads: `<web-url>`
- [ ] Aspire Dashboard accessible
- [ ] Trigger competitor price analysis scenario
- [ ] View traces in Aspire Dashboard
- [ ] Check Azure Portal for resource health

## Optional: CI/CD Setup

- [ ] Run `azd pipeline config`
- [ ] Select provider (GitHub Actions or Azure DevOps)
- [ ] Verify workflow file created
- [ ] Test automated deployment on push

## Notes

- First deployment takes 5-10 minutes
- Subsequent deployments via `azd deploy` take 2-3 minutes
- SQLite data is ephemeral (resets on container restart)
- Costs: ~$5-15/month for 2 Container Apps on consumption plan
