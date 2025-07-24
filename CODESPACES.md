# Checklist Generator - GitHub Codespaces Development

GitHub Codespaces provides an excellent development environment for this application. The live production version is running on Azure Container Instances.

## 🌐 Live Application (Azure)

**Production App:** https://checklist-generator-1753368404.eastus.azurecontainer.io:5000

*For development and testing, use Codespaces as described below.*

## 🧪 Development with Codespaces

1. **Open in Codespaces**:
   - Click the green "Code" button on your GitHub repository
   - Select "Codespaces" tab
   - Click "Create codespace on main" (or your branch)

2. **Automatic Setup**:
   - Codespaces will automatically set up the .NET environment
   - Dependencies will be restored automatically
   - Extensions will be installed

3. **Run the Application**:
   ```bash
   ./start.sh
   ```
   Or manually:
   ```bash
   cd ChecklistGenerator
   dotnet run --urls "http://0.0.0.0:5000"
   ```

4. **Access the Application**:
   - Codespaces will automatically forward port 5000
   - Click the "Open in Browser" notification
   - Or use the "Ports" tab to access the forwarded port

## 🔧 Development Features

- **Hot Reload**: Changes to C# files trigger automatic rebuilds
- **Port Forwarding**: Automatic HTTPS/HTTP port forwarding
- **VS Code Extensions**: Pre-configured with C# and .NET tools
- **Terminal Access**: Full terminal access for debugging
- **Live Testing**: Test changes before Azure deployment

## 📝 File Structure

```
.devcontainer/
  └── devcontainer.json    # Codespaces configuration
ChecklistGenerator/        # Main application
AZURE_DEPLOYMENT.md       # Azure deployment guide
Dockerfile                # Container configuration
start.sh                  # Development start script
```

## 🚀 Deployment Workflow

1. **Develop** in Codespaces
2. **Test** locally in the Codespaces environment
3. **Push** to main branch
4. **Auto-deploy** to Azure via GitHub Actions

To make your Codespace publicly accessible:
1. Go to the "Ports" tab in VS Code
2. Right-click on port 5000
3. Select "Port Visibility" → "Public"
4. Share the generated URL

## 💡 Tips

- **Free Tier**: 60 hours/month free with GitHub account
- **Persistent Storage**: Files persist between Codespace sessions
- **Environment**: Linux environment with full sudo access
- **Performance**: 2-4 CPU cores, 4-8GB RAM on free tier

## 🔗 Accessing Your App

Once running, your app will be available at:
- Local: `http://localhost:5000`
- Codespaces: `https://your-codespace-url.preview.app.github.dev`
