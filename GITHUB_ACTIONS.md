# GitHub Actions & Azure Auto-Deployment

This repository is configured with GitHub Actions for continuous integration and automated deployment to Azure Container Instances.

## 🔄 Automated Workflows

### 1. **Main Build & Deploy** (`.github/workflows/build-and-deploy.yml`)

**Triggers:**
- Push to `main` branch → **Automatic Azure deployment**
- Push to `feature/*` or `develop` branches → Build and test only  
- Pull requests to `main` → Build and test with deployment preview
- Manual dispatch with Azure deployment option

**Features:**
- ✅ .NET 9 build and test (50+ unit tests)
- 🐳 Docker image build and push to Azure Container Registry
- ☁️ **Azure Container Instances deployment**
- 🔍 Security vulnerability scanning
- 📊 Performance checks (build size analysis)
- 📢 Deployment notifications with live URLs
- 📦 Build artifact uploads

### 2. **Codespace Development** (`.github/workflows/codespace-deploy.yml`)

**Triggers:**
- Manual dispatch for development environment

**Features:**
- ✅ Quick build verification for development
- 🧪 Codespace readiness check for testing
- 📋 Deployment instruction generation

## 🛠️ Deployment Scripts

### **start.sh** - Application Startup
```bash
./start.sh
```
- Restores NuGet packages
- Builds the application
- Starts on port 5000

### **codespace-update.sh** - Auto-Update in Codespace
```bash
./codespace-update.sh
```
- Pulls latest changes from Git
- Stops running application
- Rebuilds and restarts
- Perfect for getting latest updates in active Codespace

### **webhook-deploy.sh** - Webhook Handler
```bash
./webhook-deploy.sh
```
- Checks for new commits
- Auto-updates if changes detected
- Can be triggered by webhooks

## 🚀 Deployment Process

### **Automatic (Recommended)**

1. **Push changes** to any branch:
   ```bash
   git add .
   git commit -m "Your changes"
   git push origin feature/your-branch
   ```

2. **GitHub Actions runs automatically**:
   - Builds and tests your code
   - Creates deployment artifacts
   - Runs security scans
   - Provides deployment status

3. **Deploy to Codespace**:
   - Go to your GitHub repository
   - Click `Code` → `Codespaces`
   - Create new or restart existing Codespace
   - Run `./start.sh`

### **Manual Deployment**

1. **Trigger manual workflow**:
   - Go to `Actions` tab in GitHub
   - Select "Build and Deploy Checklist Generator"
   - Click "Run workflow"
   - Enable "Trigger Codespace deployment"

2. **Update existing Codespace**:
   ```bash
   ./codespace-update.sh
   ```

## 📊 Workflow Status

Check the status of your deployments:

- **Build Status**: ![Build Status](https://github.com/stephenjtyrrell/checklist-generator/workflows/Build%20and%20Deploy%20Checklist%20Generator/badge.svg)
- **Codespace Deploy**: ![Codespace Deploy](https://github.com/stephenjtyrrell/checklist-generator/workflows/Codespace%20Auto-Deploy/badge.svg)

## 🔧 Configuration Files

- `.github/workflows/build-and-deploy.yml` - Main CI/CD pipeline
- `.github/workflows/codespace-deploy.yml` - Codespace-specific deployment
- `.devcontainer/devcontainer.json` - Codespace environment setup
- `Dockerfile` - Container deployment configuration

## 🎯 Deployment Targets

### **GitHub Codespaces** (Primary)
- ✅ Free tier: 60 hours/month
- ✅ Automatic environment setup
- ✅ VS Code with extensions
- ✅ Port forwarding
- ✅ Zero configuration required

### **Container Platforms** (Alternative)
- Docker Hub
- Azure Container Instances
- Google Cloud Run
- AWS ECS

### **Platform as a Service** (Alternative)
- Railway.app
- Render.com
- Heroku

## 🔍 Monitoring & Alerts

The workflows provide:
- **Build notifications** on success/failure
- **Security alerts** for vulnerable packages
- **Performance warnings** for large builds
- **Deployment readiness** status
- **PR comments** with deployment instructions

## 🚨 Troubleshooting

**Build fails?**
- Check the Actions tab for detailed logs
- Verify .NET 9 compatibility
- Check for package vulnerabilities

**Codespace won't start?**
- Try `./codespace-update.sh`
- Check port 5000 isn't blocked
- Verify devcontainer configuration

**Application not accessible?**
- Ensure running on `0.0.0.0:5000`
- Check Ports tab in VS Code
- Verify CORS configuration

## 🎉 Ready!

Your application now has:
- ✅ Automated building and testing
- ✅ Security scanning
- ✅ One-click Codespace deployment
- ✅ Auto-update capabilities
- ✅ Multiple deployment targets

Push your changes and watch the automation work! 🚀
