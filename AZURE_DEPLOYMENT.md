# Azure Deployment Setup

This document explains how to set up automatic deployment to Azure Container Instances using GitHub Actions.

## 🔐 Required GitHub Secrets

Before the Azure deployment can work, you need to set up the following secrets in your GitHub repository:

### 1. AZURE_CREDENTIALS
Service Principal credentials for GitHub Actions to access your Azure subscription.

**Steps to create:**
1. Install Azure CLI and login: `az login`
2. Get your subscription ID: `az account show --query "id" -o tsv`
3. Create service principal:
```bash
az ad sp create-for-rbac --name "github-actions-checklist-generator" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID
```
4. Create the JSON format for GitHub secret:
```json
{
  "clientId": "YOUR_CLIENT_ID",
  "clientSecret": "YOUR_CLIENT_SECRET", 
  "subscriptionId": "YOUR_SUBSCRIPTION_ID",
  "tenantId": "YOUR_TENANT_ID",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```
5. Add this JSON as `AZURE_CREDENTIALS` secret in your GitHub repository

### 2. AZURE_CONTAINER_REGISTRY_NAME
The name of your Azure Container Registry (without .azurecr.io).

**Steps to create:**
1. Create container registry:
```bash
az acr create --resource-group checklist-generator-rg \
  --name YOUR_UNIQUE_REGISTRY_NAME \
  --sku Basic \
  --admin-enabled true
```
2. Add `YOUR_UNIQUE_REGISTRY_NAME` as `AZURE_CONTAINER_REGISTRY_NAME` secret in GitHub

## 🚀 Deployment Options

### Automatic Deployment
- **Trigger**: Push to `main` branch
- **Action**: Automatically builds Docker image and deploys to Azure Container Instances

### Manual Deployment
- **Trigger**: Go to Actions → "Build and Deploy Checklist Generator" → "Run workflow"
- **Option**: Check "Deploy to Azure Container Instances"

## 📋 Prerequisites Setup

### 1. Register Required Azure Providers
```bash
# Register Container Registry provider
az provider register --namespace Microsoft.ContainerRegistry

# Register Container Instance provider  
az provider register --namespace Microsoft.ContainerInstance

# Check registration status (wait until both show "Registered")
az provider show -n Microsoft.ContainerRegistry --query "registrationState"
az provider show -n Microsoft.ContainerInstance --query "registrationState"
```

### 2. Create Resource Group
```bash
az group create --name checklist-generator-rg --location eastus
```

### 3. Create Container Registry
```bash
az acr create --resource-group checklist-generator-rg \
  --name YOUR_UNIQUE_REGISTRY_NAME \
  --sku Basic \
  --admin-enabled true
```

## 🌐 Accessing Your Deployed App

After successful deployment, you'll get a URL like:
```
http://checklist-generator-TIMESTAMP.eastus.azurecontainer.io:5000
```

The application will be accessible on port 5000.

## 💰 Cost Information

**Azure Container Instances (Free Tier):**
- **Allowance**: 1 vCPU + 1.5 GB RAM for 1,000,000 seconds per month
- **Cost**: Free within limits, then pay-as-you-go
- **Perfect for**: Development, testing, low-traffic applications

**Azure Container Registry (Basic SKU):**
- **Cost**: ~$5/month for Basic tier
- **Storage**: 10 GB included
- **Alternative**: Use Docker Hub (free) with public repositories

## 🔧 Troubleshooting

### Provider Registration Issues
If you see "subscription is not registered" errors:
1. Run the provider registration commands above
2. Wait 2-5 minutes for registration to complete
3. Retry deployment

### InvalidOsType Error
If you see "osType is invalid" error:
- The GitHub Actions workflow has been updated to include `--os-type Linux`
- This fix is already applied in the latest workflow

### Container Deployment Fails
1. Check Azure portal for container instance logs
2. Verify image was pushed to container registry successfully
3. Check container registry credentials are correct

### GitHub Actions Secrets
1. Go to repository Settings → Secrets and Variables → Actions
2. Add required secrets with exact names listed above
3. Verify service principal has contributor access to subscription

## 🎉 Success!

Your application is now deployed and accessible at:
**http://checklist-generator-1753368404.eastus.azurecontainer.io:5000**

## 📋 GitHub Secrets Setup

To enable automated deployments, add these secrets to your GitHub repository:

### Required Secrets:
1. **AZURE_CREDENTIALS** - Service principal JSON (see steps above)
2. **AZURE_CONTAINER_REGISTRY_NAME** - Set to: `checklistgen`

### How to Add Secrets:
1. Go to your GitHub repository
2. Click Settings → Secrets and Variables → Actions  
3. Click "New repository secret"
4. Add each secret with the exact name and value

## 📚 Additional Resources

- [Azure Container Instances Documentation](https://docs.microsoft.com/en-us/azure/container-instances/)
- [GitHub Actions Azure Integration](https://docs.microsoft.com/en-us/azure/developer/github/)
- [Azure Free Account](https://azure.microsoft.com/free/)
