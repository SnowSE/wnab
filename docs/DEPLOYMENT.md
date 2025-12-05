# WNAB Deployment Guide

## Overview

This project uses **.NET Aspire** for orchestration and deploys to **Azure Container Apps**.

## Recent Fixes (Database Connection Issues)

### Problem Identified
The deployment was failing because `AppHost.cs` was trying to deploy local development resources to Azure:
- Containerized PostgreSQL with PgAdmin/PgWeb (not compatible with Azure)
- Mailpit local email testing tool (Azure doesn't support this)
- Local data volumes and host ports

### Solution Implemented
Updated `AppHost.cs` to use environment-specific resources:

**For Azure (PublishMode):**
- Azure PostgreSQL Flexible Server (managed database)
- Email connection placeholder (for future Azure Communication Services/SendGrid)

**For Local Development:**
- Containerized PostgreSQL with admin tools
- Mailpit for email testing

## Architecture

| Component | Local Development | Azure Deployment |
|-----------|------------------|------------------|
| `WNAB.API` | Container | Azure Container Apps |
| `WNAB.Web` | Container | Azure Container Apps |
| `PostgreSQL` | Container (with PgAdmin/PgWeb) | Azure PostgreSQL Flexible Server |
| `Email` | Mailpit container | Connection string placeholder |
| `WNAB.Maui` | Included | **Excluded** - Client app |

## Deploying to Azure

### Prerequisites
- Azure CLI (`az`)
- Azure Developer CLI (`azd`)
- .NET 10 SDK
- Azure subscription with permissions to create resources

### Steps

```bash
# Login to Azure
azd auth login

# Initialize (first time only)
azd init

# Deploy (provisions resources and deploys apps)
azd up
```

### What Happens During Deployment

1. **Resource Provisioning** (first time or when infrastructure changes):
   - Azure Resource Group
   - Azure Container Apps Environment
   - Azure PostgreSQL Flexible Server
   - Networking and firewall rules
   - Container registry

2. **Application Deployment**:
   - Builds Docker images for API and Web
   - Pushes images to container registry
   - Deploys to Azure Container Apps
   - Configures environment variables and connection strings
   - Applies database migrations automatically

### Manual Aspire Publish (Alternative)

If you want to inspect what will be deployed:

```powershell
cd src\WNAB.AppHost
dotnet run -- publish --output-path ./deploy-output
```

This generates deployment manifests you can review before deploying.

## Configuration

### Required Environment Variables

#### WNAB.API
- `ConnectionStrings__wnabdb` - Auto-configured by Aspire from Azure PostgreSQL
- `Keycloak__Authority` - https://engineering.snow.edu/auth/realms/SnowCollege
- `Keycloak__Audience` - "wnab-api"
- `Keycloak__RequireHttpsMetadata` - "true"

#### WNAB.Web
- `services__wnab-api__http__0` - API endpoint (auto-configured by Aspire)
- `Keycloak__Authority` - https://engineering.snow.edu/auth/realms/SnowCollege
- `Keycloak__ClientId` - "wnab-web"
- `Keycloak__ClientSecret` - **Store in Azure Key Vault!**

### Secrets Management

**Never commit secrets to source control!**

For production, use Azure Key Vault:

```powershell
# Create Key Vault
az keyvault create --name <vault-name> --resource-group <rg-name> --location <location>

# Store secrets
az keyvault secret set --vault-name <vault-name> --name "KeycloakClientSecret" --value "<secret>"
```

Reference in `appsettings.Production.json`:
```json
{
  "Keycloak": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/KeycloakClientSecret/)"
  }
}
```

## Database Migrations

### Automatic Application
The API applies migrations on startup (see `ApiProgram.cs`):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WnabContext>();
    db.Database.Migrate();
}
```

### Manual Migration (if needed)

```powershell
# Get connection string from Azure
$env:ConnectionStrings__wnabdb="<azure-postgres-connection-string>"

# Apply migrations
dotnet ef database update --project src\WNAB.Data --startup-project src\WNAB.API
```

## MAUI Client Configuration

The MAUI app is **excluded from Azure deployment** (it's a client-side app).

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Local development (localhost API) |
| `appsettings.Production.json` | Production (deployed Azure API) |

### Setting the Production API URL

After deploying, update `src/WNAB.Maui/appsettings.Production.json`:

```json
{
  "ApiBaseUrl": "https://wnab-api.<your-env>.azurecontainerapps.io/"
}
```

### Building for Production

```bash
# Android
dotnet publish src/WNAB.Maui -c Release -f net10.0-android

# Windows
dotnet publish src/WNAB.Maui -c Release -f net10.0-windows10.0.19041

# iOS
dotnet publish src/WNAB.Maui -c Release -f net10.0-ios

# macOS
dotnet publish src/WNAB.Maui -c Release -f net10.0-maccatalyst
```

## AppHost Configuration

The `AppHost.cs` conditionally configures resources based on environment:

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    // Azure resources
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
    db = postgres.AddDatabase("wnabdb");
    mailResource = builder.AddConnectionString("mailpit");
}
else
{
    // Local development resources
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume("wnab_aspire_data")
        .WithPgWeb()
        .WithPgAdmin(...);
    // ...
}

// MAUI only in development
if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddProject<Projects.WNAB_Maui>("wnab-maui")
        .WithReference(api);
}
```

## Monitoring and Troubleshooting

### View Application Logs

```powershell
# API logs
az containerapp logs show --name wnab-api --resource-group <rg> --follow

# Web logs
az containerapp logs show --name wnab-web --resource-group <rg> --follow
```

### Common Issues

#### Database Connection Fails
**Symptoms**: API throws connection errors, migrations don't apply
**Solutions**:
- Verify PostgreSQL firewall allows Azure services
- Check connection string in Container App environment variables
- Ensure database exists (should be created by Aspire)
- Review PostgreSQL logs in Azure Portal

#### API Returns 401 Unauthorized
**Symptoms**: Authentication fails, can't access protected endpoints
**Solutions**:
- Verify Keycloak server is accessible from Azure
- Check `Keycloak__Authority` matches your realm URL
- Ensure audience and issuer settings match Keycloak configuration
- Test authentication locally first

#### Web App Can't Reach API
**Symptoms**: Service discovery fails, API calls timeout
**Solutions**:
- Verify API has `.WithExternalHttpEndpoints()` in AppHost
- Check service discovery configuration in Container Apps
- Review networking/ingress settings in Azure Portal
- Ensure both apps are in same Container Apps Environment

### Health Checks

Both API and Web expose health endpoints:

```
GET https://<app-url>/health
GET https://<app-url>/alive
```

## Performance and Cost Optimization

### Development/Staging
- Use **Basic tier** PostgreSQL (1-2 vCores)
- Enable **auto-pause** for Container Apps during inactivity
- Use **consumption plan** for Container Apps

### Production
- Scale to **Standard/Premium tier** PostgreSQL
- Enable **auto-scaling** for Container Apps (based on HTTP requests)
- Implement **Azure CDN** for static content
- Configure **Application Insights** for monitoring

## Quick Reference

| Command | Description |
|---------|-------------|
| `azd up` | Provision resources and deploy |
| `azd deploy` | Redeploy apps (skip provisioning) |
| `azd down` | Tear down all resources |
| `azd env list` | List deployment environments |
| `azd env set <name>` | Switch environment |
| `azd logs` | View application logs |

## Next Steps

1. **Deploy to Azure**: Run `azd up` to create your environment
2. **Verify Health**: Check API and Web health endpoints
3. **Test Authentication**: Ensure Keycloak integration works
4. **Configure Monitoring**: Set up Application Insights
5. **Implement CI/CD**: Use GitHub Actions for automated deployments

## Additional Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure PostgreSQL Flexible Server](https://learn.microsoft.com/azure/postgresql/flexible-server/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
