# Checklist Generator

A .NET 9 web application that converts DOCX documents to interactive SurveyJS forms.

![Build Status](https://github.com/stephenjtyrrell/checklist-generator/workflows/Build%20and%20Deploy%20Checklist%20Generator/badge.svg)
![Azure Deploy](https://github.com/stephenjtyrrell/checklist-generator/workflows/Codespace%20Auto-Deploy/badge.svg)

## 🌐 Live Application

**🚀 Currently deployed and running on Azure:**
**https://checklist-generator-1753368404.eastus.azurecontainer.io:5000**

*Upload your DOCX files and convert them to interactive SurveyJS forms instantly!*

## ☁️ Azure Deployment (Recommended)

**Fully automated deployment to Azure Container Instances!**

1. **Automatic**: Push to `main` branch triggers Azure deployment
2. **Manual**: Use GitHub Actions → "Build and Deploy" → "Deploy to Azure"
3. **Free Tier**: Runs within Azure's free Container Instances allowance
4. **Scalable**: Easy to scale up as needed

### 🔧 Setup Azure Deployment
See [AZURE_DEPLOYMENT.md](./AZURE_DEPLOYMENT.md) for complete setup instructions.

## 🧪 Alternative: GitHub Codespaces Development

For development and testing:

1. **Open Codespaces**: Click "Code" → "Codespaces" → "Create codespace"
2. **Auto-setup**: Environment configures automatically
3. **Start**: Run `./start.sh` in the terminal
4. **Access**: Use the forwarded port URL

### 🔄 Codespace Updates
```bash
./codespace-update.sh  # Pull latest changes and restart
```

## ✨ Features

- **DOCX Upload**: Upload Word documents (.docx format only)
- **Excel Conversion**: Automatically converts to Excel format in memory
- **SurveyJS Output**: Generates interactive forms from document content
- **Download Support**: Download the converted Excel file
- **Cloud Ready**: Deployed on Azure Container Instances with auto-scaling
- **CI/CD Pipeline**: Automated building, testing, and Azure deployment

## 🛠️ Local Development

```bash
cd ChecklistGenerator
dotnet restore
dotnet run
```

Visit `http://localhost:5000`

## 📁 Project Structure

- `ChecklistGenerator/` - Main .NET application
- `AZURE_DEPLOYMENT.md` - Azure deployment setup guide
- `.devcontainer/` - Codespaces configuration
- `Dockerfile` - Container deployment
- `start.sh` - Local development script

## 🌐 Hosting Options

- **Azure Container Instances** ⭐ **Current deployment** (Free tier available)
- **GitHub Codespaces** (Development environment - Free: 60 hours/month)
- **Azure App Service** (Alternative Azure option)
- **Railway.app** (Easy deployment from GitHub)
- **Render.com** (Free tier available)

## 📝 Usage

1. Open the application in your browser
2. Upload a .docx file using the upload form
3. Click "Convert to SurveyJS" to process the document
4. Download the converted Excel file if needed
5. Copy the generated SurveyJS JSON for use in your forms

## 🔧 Technology Stack

- **.NET 9** - Web API and backend
- **DocumentFormat.OpenXml** - DOCX processing
- **ClosedXML** - Excel generation
- **NPOI** - Additional Excel support
- **SurveyJS** - Form generation format

## 🧪 Testing

The project includes comprehensive unit tests covering:
- ✅ **Models**: Complete coverage of data models and DTOs
- ✅ **Services**: Core business logic and document processing
- ✅ **Integration**: End-to-end API testing
- ✅ **CI/CD**: Automated testing in GitHub Actions

```bash
# Run tests locally
cd ChecklistGenerator.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Project Status

- ✅ **Core Features**: Document conversion pipeline fully functional
- ✅ **Testing**: Comprehensive unit test coverage (50+ tests)
- ✅ **CI/CD**: Automated build, test, and Azure deployment
- ✅ **Documentation**: Complete setup and usage guides
- ✅ **Production**: Live on Azure Container Instances
- ✅ **Free Hosting**: Running within Azure free tier limits

---

**🌐 Live Application:** https://checklist-generator-1753368404.eastus.azurecontainer.io:5000

**📋 Azure Setup:** [AZURE_DEPLOYMENT.md](./AZURE_DEPLOYMENT.md)
