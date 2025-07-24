# Codespace Troubleshooting Guide

## Common Issues and Solutions

### ❌ "unable to find user vscode: no matching entries in passwd file"

**Problem**: The devcontainer configuration references a `vscode` user that doesn't exist in the base Docker image.

**Solutions**:

#### Option 1: Use Updated devcontainer.json (Recommended)
The current `devcontainer.json` includes the `common-utils` feature that creates the vscode user automatically.

#### Option 2: Use Simple Configuration (Fallback)
If the advanced configuration fails, rename files:
```bash
mv .devcontainer/devcontainer.json .devcontainer/devcontainer-advanced.json
mv .devcontainer/devcontainer-simple.json .devcontainer/devcontainer.json
```

#### Option 3: Remove User Specification
Edit `.devcontainer/devcontainer.json` and remove the line:
```json
"remoteUser": "vscode"
```

### ❌ Container Setup Errors

**Problem**: Container fails to start or configure properly.

**Solutions**:

1. **Rebuild Container**:
   - Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
   - Type "Codespaces: Rebuild Container"
   - Select and run

2. **Clear Codespace**:
   - Delete the current Codespace
   - Create a new one from the repository

3. **Check Logs**:
   - Go to View → Output
   - Select "Dev Containers" from dropdown
   - Review error messages

### ❌ Application Won't Start

**Problem**: `./start.sh` fails or application doesn't start.

**Solutions**:

1. **Manual Setup**:
   ```bash
   cd ChecklistGenerator
   dotnet restore
   dotnet build
   dotnet run --urls "http://0.0.0.0:5000"
   ```

2. **Check Dependencies**:
   ```bash
   dotnet --version  # Should be 9.0+
   dotnet restore --verbosity detailed
   ```

3. **Port Issues**:
   ```bash
   # Kill existing processes
   pkill -f "dotnet"
   # Try different port
   dotnet run --urls "http://0.0.0.0:5001"
   ```

### ❌ Port Forwarding Issues

**Problem**: Can't access application via forwarded port.

**Solutions**:

1. **Check Ports Tab**:
   - Look for "Ports" tab in VS Code
   - Ensure port 5000 is listed and public

2. **Manual Port Forward**:
   - Click "Forward a Port" in Ports tab
   - Enter `5000`
   - Set visibility to "Public"

3. **Check Application Binding**:
   ```bash
   # Ensure app binds to 0.0.0.0, not localhost
   dotnet run --urls "http://0.0.0.0:5000"
   ```

### ❌ File Permission Errors

**Problem**: Scripts aren't executable or file access denied.

**Solutions**:

```bash
# Make scripts executable
chmod +x start.sh codespace-update.sh webhook-deploy.sh

# Fix ownership if needed
sudo chown -R $USER:$USER .
```

### ❌ Build Failures

**Problem**: `dotnet build` or `dotnet restore` fails.

**Solutions**:

1. **Clear NuGet Cache**:
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --force
   ```

2. **Check .NET Version**:
   ```bash
   dotnet --info
   # Should show .NET 9.0+
   ```

3. **Manual Package Install**:
   ```bash
   cd ChecklistGenerator
   dotnet add package DocumentFormat.OpenXml
   dotnet add package ClosedXML
   dotnet add package NPOI
   ```

## Quick Fixes

### Reset Everything
```bash
# Stop all processes
pkill -f "dotnet" || true

# Clean and rebuild
cd ChecklistGenerator
dotnet clean
dotnet restore --force
dotnet build --verbosity normal

# Start fresh
dotnet run --urls "http://0.0.0.0:5000"
```

### Alternative Startup
If `./start.sh` fails:
```bash
cd ChecklistGenerator
dotnet watch run --urls "http://0.0.0.0:5000"
```

### Emergency Fallback
Create minimal startup:
```bash
echo '#!/bin/bash
cd ChecklistGenerator
dotnet run --urls "http://0.0.0.0:5000"' > simple-start.sh
chmod +x simple-start.sh
./simple-start.sh
```

## Getting Help

1. **Check GitHub Actions**: Verify builds are passing
2. **Repository Issues**: Create issue at https://github.com/stephenjtyrrell/checklist-generator
3. **Codespace Docs**: https://docs.github.com/en/codespaces

## Working Configurations

### Minimal devcontainer.json
```json
{
  "name": "Checklist Generator",
  "image": "mcr.microsoft.com/dotnet/sdk:9.0",
  "forwardPorts": [5000],
  "postCreateCommand": "cd ChecklistGenerator && dotnet restore"
}
```

### Simple Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app
COPY . .
RUN cd ChecklistGenerator && dotnet restore && dotnet build
EXPOSE 5000
CMD ["dotnet", "run", "--project", "ChecklistGenerator", "--urls", "http://0.0.0.0:5000"]
```

Most issues are resolved by rebuilding the container or using the simpler configuration files provided.
