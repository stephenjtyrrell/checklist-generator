# 📋 AI-Powered Checklist Generator

A .NET 9 web application that converts DOCX documents to interactive SurveyJS forms using OpenRouter.ai free models for intelligent document analysis and checklist generation.

![Build Status](https://github.com/stephenjtyrrell/checklist-generator/workflows/Build%20and%20Deploy%20Checklist%20Generator/badge.svg)
![Azure Deploy](https://github.com/stephenjtyrrell/checklist-generator/workflows/Codespace%20Auto-Deploy/badge.svg)

## 🌐 Live Application

**🚀 Currently deployed and running:**
- **Production**: https://checklist.stephentyrrell.ie ⭐ (Custom Domain - HTTPS)
- **Azure URL**: https://checklist-generator-stable.eastus.azurecontainer.io
- **HTTP Fallback**: http://checklist.stephentyrrell.ie

*Upload your DOCX files and convert them to interactive SurveyJS forms instantly using AI-powered analysis!*

> **🤖 AI-Enhanced**: This application now uses OpenRouter.ai free models to intelligently analyze document content, extract actionable items, and generate comprehensive checklists. No more regex patterns or static parsing - the AI understands context and creates meaningful, structured forms from any document type.

---

## ✨ AI-Powered Features

### 🎯 Complete AI Integration (Recently Updated)
All document processing has been migrated from static regex patterns to **OpenRouter.ai free models** for intelligent analysis:

- **🧠 Intelligent Document Analysis**: OpenRouter AI understands document structure and content context
- **📝 Smart Checklist Generation**: Automatically identifies actionable items, requirements, and compliance points
- **🎯 Context-Aware Processing**: AI preserves important regulatory and procedural information
- **🔄 Enhanced SurveyJS Conversion**: Better form generation with appropriate question types
- **📊 Structured Excel Output**: AI-generated Excel files with proper categorization and formatting

### 🔧 Technical AI Implementation
- **OpenRouterService**: Centralized AI service using OpenRouter.ai API with free model access
- **ExcelProcessor**: AI-powered Excel content analysis and checklist extraction
- **SurveyJSConverter**: Intelligent form generation with question type detection
- **DocxToExcelConverter**: Enhanced document processing with AI-driven content understanding
- **Error Handling**: Robust fallbacks and graceful degradation when AI is unavailable

### 🚀 Benefits Over Previous System
- **No More Regex**: Replaced complex pattern matching with intelligent content understanding
- **Flexible Processing**: Works with any document structure or content type
- **Better Accuracy**: AI understands context, relationships, and document intent
- **Improved Output**: More relevant and actionable checklist items
- **Future-Proof**: Easy to enhance and adapt AI prompts for specific use cases
- **Cost-Effective**: Uses OpenRouter.ai free models instead of paid services

---

## 📚 Table of Contents

1. [Quick Start](#-quick-start)
2. [AI Configuration](#-ai-configuration)
3. [Features](#-features)
4. [API Endpoints](#-api-endpoints)
5. [Architecture](#-architecture)
6. [Document Processing Workflow](#-document-processing-workflow)
7. [Deployment Options](#-deployment-options)
8. [Local Development](#-local-development)
9. [Azure Setup](#-azure-setup)
9. [Custom Domain Configuration](#-custom-domain-configuration)
10. [Cloudflare Integration](#-cloudflare-integration)
11. [GitHub Codespaces](#-github-codespaces)
12. [Testing](#-testing)
13. [Troubleshooting](#-troubleshooting)
14. [Technology Stack](#-technology-stack)
15. [Project Status](#-project-status)
16. [Getting Started Checklist](#-getting-started-checklist)

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

## 📋 API Endpoints

### POST /api/checklist/upload
Upload and convert a Word document to SurveyJS format with automatic format detection and conversion.

**Request:** 
- Content-Type: multipart/form-data
- Body: Word document file (.docx)

**Response:** 
```json
{
  "success": true,
  "fileName": "document.docx",
  "itemCount": 5,
  "surveyJS": { /* SurveyJS JSON format */ },
  "excelDownloadId": "guid-string",
  "excelFileName": "document_20250726_103254.xlsx",
  "message": "Successfully processed document using DOCX to Excel conversion.",
  "hasIssues": false
}
```

### GET /api/checklist/downloadExcel/{downloadId}
Download the generated Excel file using the download ID from the upload response.

**Parameters:**
- `downloadId`: The download ID returned from the upload response

**Response:** 
- Excel file download (.xlsx format)

### GET /api/checklist/sample
Get a sample SurveyJS JSON for testing purposes.

**Response:** 
```json
{
  "success": true,
  "surveyJS": { /* Sample survey JSON in SurveyJS format */ }
}
```

### GET /api/checklist/samples
Get available sample documents for testing.

**Response:** 
```json
{
  "success": true,
  "samples": [
    {
      "fileName": "ucits-section2.docx",
      "displayName": "UCITS Section 2",
      "description": "European investment fund compliance checklist",
      "icon": "📊",
      "downloadUrl": "/samples/ucits-section2.docx"
    }
  ]
}
```

### POST /api/checklist/saveResults
Save survey response data for analysis and record keeping.

**Request:** 
- Content-Type: application/json
- Body: 
```json
{
  "surveyData": {
    "item_1": "Company ABC",
    "item_2": true,
    "item_3": "UCITS"
  },
  "timestamp": "2025-07-26T10:30:00.000Z"
}
```

**Response:** 
```json
{
  "success": true,
  "id": "unique-submission-id",
  "message": "Survey results saved successfully",
  "timestamp": "2025-07-26T10:30:00.000Z"
}
```

### GET /health
Health check endpoint provided by nginx for monitoring and load balancing.

**Response:** 
```
healthy
```

**Error Response (All Endpoints):**
```json
{
  "success": false,
  "message": "Error description",
  "details": "Additional error information"
}
```

---

## 🤖 AI Configuration

### Getting Started with OpenRouter.ai

1. **Get an OpenRouter API Key**:
   - Visit [OpenRouter.ai](https://openrouter.ai/keys)
   - Create a free account
   - Generate a new API key
   - Copy the key

2. **Configure the Application**:
   
   **For Local Development (Recommended - Secure):**
   ```bash
   # Use .NET User Secrets (keeps API key out of source control)
   dotnet user-secrets init
   dotnet user-secrets set "OpenRouterApiKey" "your_actual_api_key_here"
   ```
   
   **Alternative for Local Development:**
   ```bash
   # Copy example file and update with your key
   cp appsettings.example.json appsettings.local.json
   # Edit appsettings.local.json and add your API key
   # Note: appsettings.local.json is excluded from git
   ```
   
   **For Production/Azure:**
   ```bash
   # Set environment variable
   export OpenRouterApiKey="your_actual_api_key_here"
   
   # Or in Azure Container Instances
   az container create \
     --environment-variables OpenRouterApiKey="your_actual_api_key_here"
   ```

   **⚠️ Security Note**: Never commit API keys to source control. The main `appsettings.json` should not contain sensitive values.

3. **Verify Configuration**:
   - Upload a test DOCX file
   - Check logs for "processing with OpenRouter AI" messages
   - Confirm AI-generated checklist items in response

### Rate Limiting & Free Tier Considerations

**Free Model Limitations:**
- Free models have rate limits (typically 1-10 requests per minute)
- High demand periods may cause temporary delays
- The application automatically handles rate limits with:
  - **Fallback Models**: Tries multiple free models when one is rate limited
  - **Graceful Degradation**: Shows helpful error messages during high demand
  - **Automatic Retry**: Built-in delays and retry logic

**Handling Rate Limits:**
```
Rate limit exceeded: limit_rpm/meta-llama/llama-3.2-3b-instruct/...
```

When you see this error:
1. **Wait 1-2 minutes** and try again
2. **Application will automatically try fallback models**
3. **Consider upgrading** to OpenRouter paid tier for higher limits
4. **Use during off-peak hours** for better availability

**Improving Reliability:**
- **Paid Plans**: OpenRouter offers paid plans with higher rate limits
- **Model Selection**: Configure different models in settings
- **Batch Processing**: Process multiple documents during off-peak hours

### AI Processing Features

**Core Services Enhanced with AI:**
- **OpenRouterService.cs**: New centralized AI service handling all OpenRouter.ai API interactions using free models
- **ExcelProcessor.cs**: Completely rewritten to use AI for content extraction and analysis
- **SurveyJSConverter.cs**: Enhanced with AI-powered form generation and question type detection
- **DocxToExcelConverter.cs**: Upgraded with AI-driven document understanding and structuring

**AI Capabilities:**
- **Document Analysis**: Extracts meaningful content from complex documents with context understanding
- **Checklist Generation**: Creates actionable items with proper categorization and priority
- **SurveyJS Conversion**: Generates appropriate question types and logical form structures
- **Content Interpretation**: Understands regulatory text, procedures, and compliance requirements
- **Error Handling**: Graceful fallbacks when AI processing fails, with comprehensive logging

**Performance & Reliability:**
- **Async Processing**: Non-blocking AI calls for better application responsiveness
- **HTTP Client Pool**: Efficient connection management for OpenRouter API requests
- **Retry Logic**: Built-in error handling and retry mechanisms
- **Fallback Systems**: Graceful degradation when AI services are unavailable
- **Free Models**: Cost-effective solution using OpenRouter.ai's free tier models
- **Rate Limit Handling**: Automatic fallback to alternative models when rate limits are hit
- **Model Redundancy**: Multiple free models available as fallbacks for high availability

---

## 🔄 Document Processing Workflow

The application uses a sophisticated multi-stage processing workflow:

### Stage 1: DOCX Processing & Validation
- **File Upload**: Secure handling with format validation (.docx only)
- **Size Limits**: 50MB maximum file size for optimal performance
- **Security**: In-memory processing without persistent storage
- **Format Detection**: Automatic validation of DOCX structure

### Stage 2: DOCX to Excel Conversion
- **Modern Processing**: Direct processing of .docx files using DocumentFormat.OpenXml
- **Structured Extraction**: Conversion to Excel format for better data analysis
- **Content Preservation**: Maintains formatting, tables, and structure
- **In-Memory Generation**: Creates Excel file for download without disk storage

### Stage 3: Content Analysis & Extraction
The application intelligently analyzes Excel data for:
- **Question Detection**: Text patterns indicating questions (?, "please", interrogative words)
- **Table Processing**: Structured data from tables with question/answer relationships
- **Form Elements**: Recognition of checkboxes, input fields, and interactive components
- **List Recognition**: Numbered, lettered, or bulleted option lists
- **Required Fields**: Detection of mandatory field indicators (*, "required", "mandatory")
- **Context Understanding**: Relationships between questions, choices, and sections

### Stage 4: SurveyJS Conversion & Optimization
- **Type Inference**: Automatic determination of appropriate question types:
  - **Text**: Open-ended questions and text input
  - **Boolean**: Yes/No questions and binary choices
  - **Radio Group**: Single selection from multiple options
  - **Checkbox**: Multiple selection capabilities
  - **Dropdown**: Selection from dropdown lists
  - **Comment**: Large text areas for detailed responses
- **Structure Optimization**: Logical grouping and ordering of survey elements
- **Validation**: Ensuring generated JSON meets SurveyJS schema requirements
- **Metadata Generation**: Title, description, and configuration settings

### Stage 5: Interactive Preview & Testing
- **Real-time Rendering**: Generated JSON rendered using actual SurveyJS library
- **Response Testing**: Complete surveys directly in the application
- **Validation Testing**: Test required fields and validation rules
- **Export Capabilities**: Download responses and survey definitions

---

## ✨ Features

### Core Functionality
- **DOCX Upload**: Upload Word documents (.docx format only)
- **Intelligent Content Extraction**: Automatically extract checklist items and questions from complex documents
- **Excel Conversion**: Automatically converts DOCX to Excel format in memory for structured processing
- **SurveyJS Output**: Generates industry-standard interactive forms from document content
- **Interactive Survey Preview**: Real-time preview of generated surveys using actual SurveyJS rendering
- **Survey Response Testing**: Complete and test surveys directly in the application
- **Response Export**: Save and export survey responses in JSON format
- **Download Support**: Download the converted Excel file for reference
- **Sample Documents**: Access built-in sample documents for testing
- **In-Memory Processing**: Files are processed securely in memory without local storage

### Advanced Processing Features
- **Smart Question Detection**: Identifies questions (text ending with ?, starting with "please", etc.)
- **Table Processing**: Extracts structured data from tables with question/answer pairs
- **Form Elements Recognition**: Detects checkboxes, form fields, and interactive elements
- **List Recognition**: Processes numbered or lettered option lists and choice structures
- **Required Field Detection**: Identifies indicators like *, "required", "mandatory"
- **Context Understanding**: Maintains relationships between questions and answer choices
- **Multiple Question Types**: Supports text, boolean, radio, checkbox, dropdown, and comment types

### Enterprise Features
- **HTTPS Support**: SSL encryption with certificates
- **Custom Domain**: Professional branding (checklist.stephentyrrell.ie)
- **nginx Reverse Proxy**: Production-ready with security headers
- **Auto-scaling**: Azure Container Instances with scaling
- **CI/CD Pipeline**: Automated building, testing, and deployment
- **Global CDN**: Cloudflare integration for worldwide performance
- **DDoS Protection**: Enterprise-level security
- **Health Monitoring**: Built-in health check endpoints

### Developer Features
- **Comprehensive Testing**: 50+ unit tests with coverage reports
- **Multiple Deployment Options**: Azure, Codespaces, Local
- **Docker Support**: Containerized for consistent environments
- **GitHub Actions**: Automated workflows and deployment
- **Service-Oriented Architecture**: Clean separation of concerns with dedicated services

---

## 🏗️ Architecture

### High-Level Architecture
```
Internet → Cloudflare CDN → nginx (80/443) → .NET App (5000)
         ↗ SSL Termination     ↗ HTTP/HTTPS        ↗ Application Logic
         ↘ DDoS Protection     ↘ Load Balancing    ↘ Document Processing
```

### Service-Oriented Architecture

The application follows a clean, service-oriented architecture with clear separation of concerns:

#### Core Services
- **DocxToExcelConverter**: Converts .docx files to Excel format for structured processing
- **ExcelProcessor**: Processes Excel files and extracts structured content to identify checklist items
- **SurveyJSConverter**: Converts extracted content to SurveyJS JSON format

#### Controllers
- **ChecklistController**: REST API endpoints for file upload, processing, and download operations

#### Models
- **ChecklistItem**: Data model for extracted checklist items with type information
- **SurveyJSForm**: Models for SurveyJS schema and survey elements

#### Processing Pipeline
1. **File Upload & Validation**: Secure file handling with format detection
2. **Document to Excel Conversion**: Convert DOCX files to Excel format for structured data extraction
3. **Content Analysis**: Intelligent parsing of Excel data to extract questions and form elements
4. **Data Transformation**: Conversion to SurveyJS-compatible format
5. **Response Generation**: JSON output with comprehensive status information

### Technology Stack
- **.NET 9**: Modern web framework with minimal APIs and high performance
- **DocumentFormat.OpenXml**: Modern .docx document processing and manipulation
- **ClosedXML**: Excel generation and formatting
- **NPOI**: Additional Excel support and compatibility
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
- ✅ **In-Memory Processing** (No persistent file storage)
- ✅ **CORS Configuration** (Controlled cross-origin access)

---

## 🌍 Deployment Options

### ⭐ Production (Current)
- **URL**: https://checklist.stephentyrrell.ie
- **Platform**: Azure Container Instances + Cloudflare
- **SSL**: Trusted certificates via Cloudflare
- **Performance**: Global CDN with caching
- **Cost**: FREE (within Azure and Cloudflare free tiers)
- **Features**: Full enterprise features with health monitoring

### ☁️ Azure Container Instances
- **Automatic**: Push to `main` branch triggers deployment
- **Manual**: Use GitHub Actions → "Build and Deploy"
- **Scaling**: Easy to scale up as needed
- **Monitoring**: Azure built-in monitoring
- **Stable DNS**: Uses `checklist-generator-stable.eastus.azurecontainer.io`

### 🧪 GitHub Codespaces (Development)
- **Purpose**: Development and testing
- **Setup**: Automatic environment configuration
- **Access**: Forwarded port URL with HTTPS
- **Cost**: Free (60 hours/month)
- **Features**: Full VS Code environment with pre-configured dependencies

### 🏠 Local Development
- **Requirements**: .NET 9 SDK
- **Port**: http://localhost:5000
- **Hot Reload**: Automatic during development
- **Database**: No external dependencies required

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

#### 3. `OPENROUTER_API_KEY`
Your OpenRouter.ai API key for AI-powered document processing (free models available)

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

# Deploy container with stable DNS name
az container create \
  --resource-group checklist-generator-rg \
  --name checklist-generator \
  --image YOUR_REGISTRY.azurecr.io/checklist-generator:latest \
  --ports 80 443 \
  --environment-variables OpenRouterApiKey="YOUR_OPENROUTER_API_KEY" \
  --dns-name-label checklist-generator-stable
```

---

## 🌐 Custom Domain Configuration

### Current Setup: checklist.stephentyrrell.ie

The application is configured with a professional custom domain using DNS CNAME records.

### Stable DNS Configuration (Updated 2024)
The container now uses a **stable DNS label** that doesn't change between deployments:

```
Type: CNAME
Name: checklist
Value: checklist-generator-stable.eastus.azurecontainer.io
TTL: 300
```

⚠️ **Action Required**: Update your DNS CNAME record to point to the new stable FQDN above.

### Legacy DNS (Will be deprecated)
```
Type: CNAME
Name: checklist  
Value: checklist-generator-1753371092.eastus.azurecontainer.io (timestamp-based - changes on redeploy)
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
Value: checklist-generator-stable.eastus.azurecontainer.io
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
  - `ChecklistItemTests.cs` - ChecklistItem model and ChecklistItemType enum
  - `SurveyJSFormTests.cs` - All SurveyJS-related models
- ✅ **Services**: Core business logic and document processing
  - `SurveyJSConverterTests.cs` - SurveyJS format conversion
  - `DocxToExcelConverterTests.cs` - DOCX to Excel conversion functionality
  - `ExcelProcessorTests.cs` - Excel file processing and content extraction
- ✅ **Controllers**: API endpoint testing
  - `ChecklistControllerTests.cs` - Unit tests with mocked dependencies
- ✅ **Integration**: End-to-end API testing
  - `ChecklistControllerIntegrationTests.cs` - Full workflow testing
- ✅ **Configuration**: Application setup validation
  - `StartupConfigurationTests.cs` - Dependency injection and configuration
- ✅ **Edge Cases**: Extreme scenarios and error handling
  - `EdgeCaseTests.cs` - Large files, unicode, malformed content
- ✅ **Build Validation**: Infrastructure testing
  - `BuildValidationTests.cs` - Package references and build configuration

### Test Categories
- **Unit Tests**: Individual component testing with 90%+ coverage
- **Integration Tests**: End-to-end workflow testing
- **Security Tests**: Input validation and error handling
- **Performance Tests**: Large file handling and memory management
- **Unicode Support**: International character and special symbol testing

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

# Run specific test class
dotnet test --filter "ChecklistControllerTests"

# Run with verbose output
dotnet test --verbosity normal
```

#### GitHub Actions
Tests run automatically on:
- Every push to main branch
- All pull requests
- Manual workflow dispatch
- Feature branch pushes

#### Coverage Reports
- **HTML Reports**: Detailed analysis with line-by-line coverage
- **Cobertura Format**: CI integration and metrics
- **Text Summary**: Quick overview in console output

### Test Dependencies
- **xUnit**: Primary testing framework
- **FluentAssertions**: Fluent assertion library for readable tests
- **Moq**: Mocking framework for dependency isolation
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing support
- **coverlet.collector**: Code coverage collection

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
1. **Health Check**: Visit `/health` endpoint (https://checklist.stephentyrrell.ie/health)
2. **File Upload Issues**: Check file size (<50MB) and format (.docx only)
3. **Memory Issues**: Restart container if processing large files
4. **Processing Errors**: Check logs for detailed error information

### Application File Structure

The project follows a clean, organized structure:

```
ChecklistGenerator/
├── Controllers/
│   └── ChecklistController.cs        # REST API endpoints and request handling
├── Models/
│   ├── ChecklistItem.cs             # Data models for extracted content
│   └── SurveyJSForm.cs              # SurveyJS schema models and DTOs
├── Services/
│   ├── DocxToExcelConverter.cs      # .docx to Excel conversion service
│   ├── ExcelProcessor.cs            # Excel file parsing and content extraction
│   └── SurveyJSConverter.cs         # JSON conversion and SurveyJS formatting
├── wwwroot/
│   ├── index.html                   # Main web interface with drag-and-drop
│   └── samples/                     # Sample DOCX files for testing
│       ├── ucits-section2.docx
│       └── ucits-section3.docx
├── Program.cs                       # Application configuration and DI setup
└── ChecklistGenerator.csproj        # Project dependencies and configuration

ChecklistGenerator.Tests/
├── Models/
│   ├── ChecklistItemTests.cs
│   └── SurveyJSFormTests.cs
├── Services/
│   ├── SurveyJSConverterTests.cs
│   ├── DocxToExcelConverterTests.cs
│   └── ExcelProcessorTests.cs
├── Controllers/
│   └── ChecklistControllerTests.cs
├── Integration/
│   └── ChecklistControllerIntegrationTests.cs
└── TestData/
    └── test-document.docx

Infrastructure/
├── Dockerfile                      # Multi-stage container build
├── nginx.conf                      # Reverse proxy and SSL configuration
├── supervisord.conf                 # Process management configuration
├── start-container.sh              # Container startup script
├── start.sh                        # Local development startup
└── .github/workflows/              # CI/CD automation
    ├── build-and-deploy.yml
    └── codespace-deploy.yml
```

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
2. **GitHub Issues**: Create issue in repository for bugs or feature requests
3. **Azure Monitoring**: Check Azure portal for container metrics and logs
4. **Cloudflare Analytics**: Monitor traffic and performance metrics
5. **Application Logs**: Use Azure Container Instances log stream for debugging

---

## 🛠️ Technology Stack

### Backend (.NET 9)
- **ASP.NET Core 9.0**: Modern web framework with minimal APIs and high performance
- **DocumentFormat.OpenXml 3.3.0**: Modern .docx document processing and manipulation
- **ClosedXML 0.104.1**: Excel generation and formatting
- **NPOI 2.7.4**: Additional Excel support and compatibility
- **Built-in JSON Serialization**: SurveyJS format generation
- **Dependency Injection**: Service-oriented architecture with scoped services

### Frontend & User Interface
- **HTML/CSS/JavaScript**: Responsive web interface with drag-and-drop support
- **SurveyJS Library**: Interactive survey rendering and response collection
- **CDN Resources**: SurveyJS core and UI libraries loaded from unpkg CDN
- **Bootstrap**: Modern responsive design framework
- **Real-time Preview**: Client-side survey rendering and testing

### Infrastructure & Deployment
- **nginx**: High-performance reverse proxy and load balancer
- **Docker**: Multi-stage containerization for consistent deployments
- **Azure Container Instances**: Managed container hosting with auto-scaling
- **Cloudflare**: CDN, SSL, and security services
- **GitHub Actions**: CI/CD automation and deployment pipelines
- **Supervisor**: Process management for nginx and .NET application

### Development & Testing
- **xUnit**: Comprehensive unit testing framework
- **FluentAssertions**: Readable and maintainable test assertions
- **Moq**: Mocking framework for isolated testing
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing capabilities
- **coverlet.collector**: Code coverage analysis and reporting
- **GitHub Codespaces**: Cloud development environment
- **VS Code**: Recommended IDE with dev container support

### Monitoring & Analytics
- **Azure Monitor**: Container and application monitoring
- **Cloudflare Analytics**: Traffic and performance metrics
- **GitHub Actions**: Build and deployment monitoring
- **nginx Health Checks**: Built-in health monitoring endpoints
- **Structured Logging**: Comprehensive application logging with ILogger

### Security & Performance
- **SSL/TLS**: Cloudflare-managed certificates with automatic renewal
- **Security Headers**: HSTS, XSS protection, content security policies
- **Rate Limiting**: nginx-based request throttling
- **DDoS Protection**: Cloudflare enterprise-grade protection
- **CORS Configuration**: Controlled cross-origin resource sharing
- **Input Validation**: File type, size, and content validation
- **In-Memory Processing**: Secure file handling without persistent storage

### File Processing Technologies
- **DocumentFormat.OpenXml**: Native .docx reading and manipulation
- **NPOI**: Excel file creation and data extraction
- **ClosedXML**: High-level Excel operations and formatting
- **Stream Processing**: Efficient memory management for large files
- **Async/Await**: Non-blocking I/O operations

---

## 📊 Project Status

### ✅ Completed Features
- **Core Application**: Document conversion pipeline fully functional
  - DOCX to Excel conversion with DocumentFormat.OpenXml
  - Intelligent content extraction and question detection
  - SurveyJS format generation with multiple question types
  - Real-time survey preview and testing capabilities
- **Testing Suite**: Comprehensive unit test coverage (50+ tests)
  - Models, Services, Controllers, and Integration tests
  - Edge case handling and unicode support
  - Build validation and configuration testing
- **CI/CD Pipeline**: Automated build, test, and Azure deployment
  - GitHub Actions workflows for continuous deployment
  - Automated testing on pull requests and pushes
  - Multi-environment deployment support
- **Production Deployment**: Live on Azure with custom domain
  - Stable DNS configuration with Azure Container Instances
  - Custom domain (checklist.stephentyrrell.ie) with professional branding
- **SSL/HTTPS**: Trusted certificates via Cloudflare
  - Automatic certificate management and renewal
  - Full SSL encryption and security headers
- **API Infrastructure**: Complete REST API with comprehensive endpoints
  - File upload and processing endpoints
  - Sample document management
  - Excel download capabilities
  - Survey response saving and export
- **Documentation**: Complete setup and usage guides
  - Comprehensive README with step-by-step instructions
  - API documentation with examples
  - Troubleshooting guides and deployment procedures
- **Performance**: Optimized with CDN and caching
  - Global CDN via Cloudflare
  - In-memory processing for optimal performance
  - Efficient file handling and memory management
- **Security**: Enterprise-grade protection and headers
  - DDoS protection, rate limiting, and input validation
  - Secure file processing without persistent storage

### 🎯 Architecture Highlights
- **Scalable**: Container-based with auto-scaling capabilities
  - Azure Container Instances with horizontal scaling
  - Stateless design for easy replication
- **Secure**: HTTPS, security headers, rate limiting, DDoS protection
  - Multiple layers of security from application to infrastructure
  - Input validation and secure file processing
- **Fast**: Global CDN, compression, and optimized delivery
  - Cloudflare CDN with 200+ global data centers
  - nginx reverse proxy with performance optimizations
- **Reliable**: 99.99% uptime with Cloudflare and Azure
  - Health monitoring and automatic recovery
  - Redundant infrastructure and monitoring
- **Cost-Effective**: Runs within free tiers of Azure and Cloudflare
  - Optimized resource usage
  - No ongoing costs for small to medium usage
- **Developer-Friendly**: Clean architecture with comprehensive testing
  - Service-oriented design with clear separation of concerns
  - Extensive test coverage and documentation

### 🔄 Continuous Improvements
- **Automated Testing**: Every commit triggers full test suite
  - Comprehensive unit and integration testing
  - Code coverage reporting and quality gates
- **Security Scanning**: Automated vulnerability checking
  - Dependency scanning and security updates
  - Container security and best practices
- **Performance Monitoring**: Real-time metrics and alerting
  - Application performance monitoring
  - Infrastructure health checks and monitoring
- **Documentation**: Living documentation updated with code changes
  - Automatic documentation updates with deployments
  - Comprehensive API documentation and examples

### 🚀 Current Capabilities Summary
1. **Document Processing**: Full DOCX to SurveyJS conversion pipeline
2. **Interactive Features**: Real-time survey preview and testing
3. **API Complete**: All necessary endpoints for full functionality
4. **Production Ready**: Live deployment with enterprise features
5. **Developer Ready**: Comprehensive testing and documentation
6. **Scalable Infrastructure**: Container-based with auto-scaling
7. **Security Compliant**: Enterprise-grade security features
8. **Performance Optimized**: Global CDN and efficient processing

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
