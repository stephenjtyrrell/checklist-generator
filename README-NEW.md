# 📋 Checklist Generator

A .NET 9 web application that converts DOCX documents to interactive SurveyJS forms with enterprise-grade deployment capabilities.

![Build Status](https://github.com/stephenjtyrrell/checklist-generator/workflows/Build%20and%20Deploy%20Checklist%20Generator/badge.svg)
![Azure Deploy](https://github.com/stephenjtyrrell/checklist-generator/workflows/Codespace%20Auto-Deploy/badge.svg)

## 🌐 Live Application

**🚀 Currently deployed and running:**
- **Production**: https://checklist.stephentyrrell.ie ⭐ (Custom Domain - HTTPS)
- **Azure URL**: https://checklist-generator-1753371092.eastus.azurecontainer.io
- **HTTP Fallback**: http://checklist.stephentyrrell.ie

*Upload your DOCX files and convert them to interactive SurveyJS forms instantly!*

---

## 📚 Table of Contents

1. [Quick Start](#-quick-start)
2. [Features](#-features)
3. [Architecture](#-architecture)
4. [Deployment Options](#-deployment-options)
5. [Local Development](#-local-development)
6. [Azure Setup](#-azure-setup)
7. [Custom Domain Configuration](#-custom-domain-configuration)
8. [Cloudflare Integration](#-cloudflare-integration)
9. [GitHub Codespaces](#-github-codespaces)
10. [Testing](#-testing)
11. [Troubleshooting](#-troubleshooting)
12. [Technology Stack](#-technology-stack)

---

## 🚀 Quick Start

### Option 1: Use Live Application
Visit **https://checklist.stephentyrrell.ie** and start converting DOCX files immediately!

### Option 2: GitHub Codespaces (Development)
1. Click **Code** → **Codespaces** → **Create codespace**
2. Run `./start.sh` in the terminal
3. Access via forwarded port URL

### Option 3: Local Development
```bash
cd ChecklistGenerator
dotnet restore
dotnet run
# Visit http://localhost:5000
```

---

## ✨ Features

### Core Functionality
- **DOCX Upload**: Upload Word documents (.docx format only)
- **Excel Conversion**: Automatically converts to Excel format in memory
- **SurveyJS Output**: Generates interactive forms from document content
- **Download Support**: Download the converted Excel file

### Enterprise Features
- **HTTPS Support**: SSL encryption with certificates
- **Custom Domain**: Professional branding (checklist.stephentyrrell.ie)
- **nginx Reverse Proxy**: Production-ready with security headers
- **Auto-scaling**: Azure Container Instances with scaling
- **CI/CD Pipeline**: Automated building, testing, and deployment
- **Global CDN**: Cloudflare integration for worldwide performance
- **DDoS Protection**: Enterprise-level security

### Developer Features
- **Comprehensive Testing**: 50+ unit tests with coverage reports
- **Multiple Deployment Options**: Azure, Codespaces, Local
- **Docker Support**: Containerized for consistent environments
- **GitHub Actions**: Automated workflows and deployment

---

## 🏗️ Architecture

### High-Level Architecture
```
Internet → Cloudflare CDN → nginx (80/443) → .NET App (5000)
         ↗ SSL Termination     ↗ HTTP/HTTPS        ↗ Application Logic
         ↘ DDoS Protection     ↘ Load Balancing    ↘ Document Processing
```

### Technology Stack
- **.NET 9**: Web API and backend processing
- **nginx**: Reverse proxy and SSL termination
- **Docker**: Containerization and deployment
- **Azure Container Instances**: Cloud hosting
- **Cloudflare**: CDN, SSL, and security
- **GitHub Actions**: CI/CD automation

### Security Features
- ✅ **SSL/TLS Encryption** (TLSv1.2, TLSv1.3)
- ✅ **Security Headers** (HSTS, XSS Protection, Content-Type)
- ✅ **Rate Limiting** (10 requests/second protection)
- ✅ **DDoS Protection** (Cloudflare)
- ✅ **Input Validation** (File type and size limits)

---

## 🌍 Deployment Options

### ⭐ Production (Current)
- **URL**: https://checklist.stephentyrrell.ie
- **Platform**: Azure Container Instances + Cloudflare
- **SSL**: Trusted certificates via Cloudflare
- **Performance**: Global CDN with caching
- **Cost**: FREE (within Azure and Cloudflare free tiers)

### ☁️ Azure Container Instances
- **Automatic**: Push to `main` branch triggers deployment
- **Manual**: Use GitHub Actions → "Build and Deploy"
- **Scaling**: Easy to scale up as needed
- **Monitoring**: Azure built-in monitoring

### 🧪 GitHub Codespaces (Development)
- **Purpose**: Development and testing
- **Setup**: Automatic environment configuration
- **Access**: Forwarded port URL
- **Cost**: Free (60 hours/month)

### 🏠 Local Development
- **Requirements**: .NET 9 SDK
- **Port**: http://localhost:5000
- **Hot Reload**: Automatic during development

---

## 🔧 Local Development

### Prerequisites
- .NET 9 SDK
- Git
- Optional: Docker (for container testing)

### Setup
```bash
# Clone repository
git clone https://github.com/stephenjtyrrell/checklist-generator.git
cd checklist-generator

# Restore dependencies
cd ChecklistGenerator
dotnet restore

# Run application
dotnet run

# Visit application
open http://localhost:5000
```

### Development Commands
```bash
# Build
dotnet build

# Run tests
cd ChecklistGenerator.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Publish for deployment
dotnet publish --configuration Release
```

---

## ☁️ Azure Setup

### Required GitHub Secrets

For automated Azure deployment, configure these secrets in GitHub repository settings:

#### 1. `AZURE_CREDENTIALS`
```json
{
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret",
  "subscriptionId": "your-subscription-id",
  "tenantId": "your-tenant-id",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

#### 2. `AZURE_CONTAINER_REGISTRY_NAME`
Your Azure Container Registry name (e.g., "checklistgen")

### Create Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name checklist-generator-rg --location eastus

# Create container registry
az acr create --resource-group checklist-generator-rg \
  --name YOUR_UNIQUE_NAME --sku Basic --admin-enabled true

# Create service principal for GitHub Actions
az ad sp create-for-rbac --name "checklist-generator-github" \
  --role contributor \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID
```

### Manual Deployment
```bash
# Build and push image
az acr build --registry YOUR_REGISTRY_NAME --image checklist-generator .

# Deploy container
az container create \
  --resource-group checklist-generator-rg \
  --name checklist-generator \
  --image YOUR_REGISTRY.azurecr.io/checklist-generator:latest \
  --ports 80 443 \
  --dns-name-label checklist-generator-$(date +%s)
```

---

## 🌐 Custom Domain Configuration

### Current Setup: checklist.stephentyrrell.ie

The application is configured with a professional custom domain using DNS CNAME records.

### DNS Configuration
```
Type: CNAME
Name: checklist
Value: checklist-generator-1753371092.eastus.azurecontainer.io
TTL: 300
```

### Benefits
- ✅ **Professional Appearance**: No Azure URLs in production
- ✅ **Brand Consistency**: matches stephentyrrell.ie domain
- ✅ **SEO Friendly**: Better search engine optimization
- ✅ **SSL Ready**: Works with Cloudflare SSL

### Setting Up Your Own Domain

1. **Purchase/Configure Domain**: Get a domain name
2. **Add DNS Record**: Create CNAME pointing to Azure container
3. **Wait for Propagation**: 5-30 minutes typically
4. **Test Access**: Verify domain works
5. **Optional**: Add Cloudflare for SSL and performance

---

## 🌩️ Cloudflare Integration

### Current Setup
The production application uses Cloudflare for:
- ✅ **Free SSL Certificates** (trusted by browsers)
- ✅ **Global CDN** (faster loading worldwide)
- ✅ **DDoS Protection** (enterprise-level security)
- ✅ **Performance Optimization** (caching, compression)
- ✅ **Analytics** (visitor insights)

### Cloudflare Configuration

#### 1. Add Domain to Cloudflare
- Sign up at https://cloudflare.com (free)
- Add stephentyrrell.ie to account
- Update nameservers at domain registrar

#### 2. DNS Records
```
Type: CNAME
Name: checklist
Value: checklist-generator-1753371092.eastus.azurecontainer.io
Proxy: ✅ Proxied (orange cloud)
```

#### 3. SSL Settings
- **Encryption Mode**: Full (not Full Strict)
- **Always Use HTTPS**: Enabled
- **Min TLS Version**: 1.2
- **Automatic HTTPS Rewrites**: Enabled

#### 4. Performance Settings
- **Auto Minify**: CSS, JavaScript, HTML
- **Brotli Compression**: Enabled
- **Browser Cache TTL**: 4 hours

### Benefits
- **Free Forever**: No ongoing costs
- **Enterprise Features**: DDoS protection, WAF, analytics
- **Global Performance**: 200+ data centers worldwide
- **Reliability**: 99.99% uptime SLA

---

## 🧪 GitHub Codespaces

### Features
- **Instant Development Environment**: Pre-configured with all dependencies
- **VS Code in Browser**: Full IDE experience
- **Port Forwarding**: Access application via HTTPS URL
- **Free Tier**: 60 hours/month for free accounts

### Usage

#### Option 1: Quick Start
1. Visit https://github.com/stephenjtyrrell/checklist-generator
2. Click **Code** → **Codespaces** → **Create codespace**
3. Wait for environment setup (2-3 minutes)
4. Run `./start.sh` in terminal
5. Click forwarded port URL when prompted

#### Option 2: Customized Setup
```bash
# After codespace starts
cd ChecklistGenerator
dotnet restore
dotnet run

# Access via forwarded port 5000
```

### Configuration Files
- `.devcontainer/devcontainer.json`: Main configuration
- `.devcontainer/devcontainer-simple.json`: Fallback configuration
- `start.sh`: Application startup script

### Troubleshooting Codespaces

#### Container Setup Issues
1. **Rebuild Container**: Ctrl+Shift+P → "Codespaces: Rebuild Container"
2. **Check Logs**: View → Output → "Dev Containers"
3. **Use Simple Config**: Rename devcontainer-simple.json if needed

#### Application Issues
```bash
# Check .NET installation
dotnet --version

# Restore packages
dotnet restore

# Build project
dotnet build

# Check port availability
netstat -tulpn | grep :5000
```

---

## 🧪 Testing

### Test Coverage
The project includes comprehensive testing with 50+ unit tests covering:

- ✅ **Models**: Complete coverage of data models and DTOs
- ✅ **Services**: Core business logic and document processing
- ✅ **Integration**: End-to-end API testing
- ✅ **CI/CD**: Automated testing in GitHub Actions

### Running Tests

#### Local Testing
```bash
# Run all tests
cd ChecklistGenerator.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"./coverage"
```

#### GitHub Actions
Tests run automatically on:
- Every push to main branch
- All pull requests
- Manual workflow dispatch

### Test Categories
- **Unit Tests**: Individual component testing
- **Integration Tests**: End-to-end workflow testing
- **Security Tests**: Vulnerability scanning
- **Performance Tests**: Build size and optimization checks

---

## 🔍 Troubleshooting

### Common Issues

#### ❌ Container Won't Start
```bash
# Check container status
az container show --name checklist-generator --resource-group checklist-generator-rg

# View logs
az container logs --name checklist-generator --resource-group checklist-generator-rg

# Restart container
az container restart --name checklist-generator --resource-group checklist-generator-rg
```

#### ❌ Custom Domain Not Working
1. **Check DNS Propagation**:
   ```bash
   nslookup checklist.stephentyrrell.ie
   # Should return Azure container IP
   ```

2. **Verify DNS Record**: Ensure CNAME points to correct Azure URL
3. **Clear Browser Cache**: Hard refresh (Ctrl+F5)
4. **Check TTL Settings**: Lower TTL for faster updates

#### ❌ SSL Certificate Issues
1. **Cloudflare**: Ensure SSL mode is "Full" (not "Full Strict")
2. **Browser Warning**: Expected for self-signed certs without Cloudflare
3. **Certificate Renewal**: Self-signed certs valid for 365 days

#### ❌ Application Errors
1. **Health Check**: Visit `/health` endpoint
2. **File Upload Issues**: Check file size (<50MB) and format (.docx only)
3. **Memory Issues**: Restart container if processing large files

#### ❌ GitHub Actions Failures

##### Authentication Errors
```
Error: Login failed with Error: Using auth-type: SERVICE_PRINCIPAL
```
**Solution**: Verify GitHub secrets are correctly configured:
- `AZURE_CREDENTIALS`: Complete JSON with all required fields
- `AZURE_CONTAINER_REGISTRY_NAME`: Registry name only (no .azurecr.io)

##### Build Failures
```bash
# Check workflow status
gh run list --limit 5

# View logs for failed run
gh run view RUN_ID --log
```

##### Deployment Issues
1. **Resource Group**: Ensure exists in correct region
2. **Registry Access**: Verify container registry permissions
3. **Port Configuration**: Ensure ports 80 and 443 are exposed

### Getting Help

1. **Health Endpoint**: https://checklist.stephentyrrell.ie/health
2. **GitHub Issues**: Create issue in repository
3. **Azure Monitoring**: Check Azure portal for container metrics
4. **Cloudflare Analytics**: Monitor traffic and performance

---

## 🛠️ Technology Stack

### Backend
- **.NET 9**: Modern web framework with minimal APIs
- **DocumentFormat.OpenXml**: DOCX processing and manipulation
- **ClosedXML**: Excel generation and formatting
- **NPOI**: Additional Excel support and compatibility

### Infrastructure
- **nginx**: High-performance reverse proxy and load balancer
- **Docker**: Containerization for consistent deployments
- **Azure Container Instances**: Managed container hosting
- **Cloudflare**: CDN, SSL, and security services
- **GitHub Actions**: CI/CD automation and deployment

### Development
- **xUnit**: Unit testing framework
- **GitHub Codespaces**: Cloud development environment
- **VS Code**: Recommended IDE with dev container support
- **Git**: Version control and collaboration

### Monitoring & Analytics
- **Azure Monitor**: Container and application monitoring
- **Cloudflare Analytics**: Traffic and performance metrics
- **GitHub Actions**: Build and deployment monitoring

---

## 📊 Project Status

### ✅ Completed Features
- **Core Application**: Document conversion pipeline fully functional
- **Testing Suite**: Comprehensive unit test coverage (50+ tests)
- **CI/CD Pipeline**: Automated build, test, and Azure deployment
- **Production Deployment**: Live on Azure with custom domain
- **SSL/HTTPS**: Trusted certificates via Cloudflare
- **Documentation**: Complete setup and usage guides
- **Performance**: Optimized with CDN and caching
- **Security**: Enterprise-grade protection and headers

### 🎯 Architecture Highlights
- **Scalable**: Container-based with auto-scaling capabilities
- **Secure**: HTTPS, security headers, rate limiting, DDoS protection
- **Fast**: Global CDN, compression, and optimized delivery
- **Reliable**: 99.99% uptime with Cloudflare and Azure
- **Cost-Effective**: Runs within free tiers of Azure and Cloudflare

### 🔄 Continuous Improvements
- **Automated Testing**: Every commit triggers full test suite
- **Security Scanning**: Automated vulnerability checking
- **Performance Monitoring**: Real-time metrics and alerting
- **Documentation**: Living documentation updated with code changes

---

## 🚀 Getting Started Checklist

### For Users
- [ ] Visit https://checklist.stephentyrrell.ie
- [ ] Upload a .docx file
- [ ] Download converted Excel file
- [ ] Copy generated SurveyJS JSON

### For Developers
- [ ] Clone repository
- [ ] Set up local development environment
- [ ] Run tests locally
- [ ] Create GitHub Codespace for cloud development
- [ ] Deploy to Azure (optional)

### For Production Deployment
- [ ] Set up Azure account and resources
- [ ] Configure GitHub secrets
- [ ] Set up custom domain
- [ ] Configure Cloudflare
- [ ] Monitor application health

---

**🌐 Live Application**: https://checklist.stephentyrrell.ie

**💻 Source Code**: https://github.com/stephenjtyrrell/checklist-generator

**📧 Contact**: stephen@stephentyrrell.ie
