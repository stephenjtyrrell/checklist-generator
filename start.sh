#!/bin/bash -e

echo "🚀 Starting Checklist Generator Application..."

# Navigate to the application directory
cd ChecklistGenerator

# Restore dependencies
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build the application
echo "🔨 Building application..."
dotnet build

# Start the application
echo "🌟 Starting application on http://localhost:5000..."
echo "📝 The application will be accessible via port forwarding in Codespaces"
dotnet run --urls "http://0.0.0.0:5000"
