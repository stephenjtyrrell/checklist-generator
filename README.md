# Checklist Generator

A .NET 9 web application that converts DOCX documents to interactive SurveyJS forms.

![Build Status](https://github.com/stephenjtyrrell/checklist-generator/workflows/Build%20and%20Deploy%20Checklist%20Generator/badge.svg)
![Codespace Deploy](https://github.com/stephenjtyrrell/checklist-generator/workflows/Codespace%20Auto-Deploy/badge.svg)

## 🚀 Deploy to GitHub Codespaces (Recommended)

**Automated deployment with GitHub Actions!**

1. **Push to GitHub**: Changes automatically trigger build and deployment
2. **Open Codespaces**: Click the green "Code" button → "Codespaces" → "Create codespace"
3. **Auto-setup**: Codespaces will automatically configure everything
4. **Start**: Run `./start.sh` or use the terminal to start the app
5. **Access**: Use the forwarded port URL to access your application

### 🔄 Auto-Updates in Codespace
```bash
./codespace-update.sh  # Pull latest changes and restart
```

## ✨ Features

- **DOCX Upload**: Upload Word documents (.docx format only)
- **Excel Conversion**: Automatically converts to Excel format in memory
- **SurveyJS Output**: Generates interactive forms from document content
- **Download Support**: Download the converted Excel file
- **Cloud Ready**: Optimized for GitHub Codespaces and cloud hosting
- **CI/CD Pipeline**: Automated building, testing, and deployment

## 🛠️ Local Development

```bash
cd ChecklistGenerator
dotnet restore
dotnet run
```

Visit `http://localhost:5000`

## 📁 Project Structure

- `ChecklistGenerator/` - Main .NET application
- `.devcontainer/` - Codespaces configuration
- `Dockerfile` - Container deployment
- `start.sh` - Quick start script

## 🌐 Hosting Options

- **GitHub Codespaces** (Free tier: 60 hours/month)
- **Railway.app** (Easy deployment from GitHub)
- **Render.com** (Free tier available)
- **Azure Container Instances**
- **Google Cloud Run**

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
- ✅ **CI/CD**: Automated build, test, and deployment
- ✅ **Documentation**: Complete setup and usage guides
- ✅ **Deployment**: Ready for GitHub Codespaces and cloud hosting

---

**Ready for instant deployment on GitHub Codespaces!** 🚀
