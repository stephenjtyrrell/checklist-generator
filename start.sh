#!/bin/bash

echo "🚀 Starting Checklist Generator Application..."
echo ""
echo "🌐 Production App (Azure): https://checklist-generator-1753368404.eastus.azurecontainer.io"
echo "🧪 Development Environment: Starting locally..."
echo ""

# Navigate to the application directory
cd ChecklistGenerator

# Restore dependencies
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build the application
echo "🔨 Building application..."
dotnet build

# Start the application
echo "🌟 Starting development server on http://localhost:5000..."
echo "📝 For production, use the Azure deployment above"
echo "🔧 This local instance is for development and testing"
dotnet run --urls "http://0.0.0.0:5000"
