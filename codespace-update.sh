#!/bin/bash

# Codespace Auto-Update Script
# This script pulls latest changes and restarts the application

echo "🔄 Codespace Auto-Update Script"
echo "==============================="

# Check if we're in a Codespace
if [[ $CODESPACES == "true" ]]; then
    echo "✅ Running in GitHub Codespace"
else
    echo "⚠️  Not in Codespace environment"
fi

# Stop any running application
echo "🛑 Stopping running application..."
pkill -f "dotnet run" || true
pkill -f "ChecklistGenerator" || true

# Pull latest changes
echo "📥 Pulling latest changes..."
git fetch origin
git pull origin $(git branch --show-current)

# Navigate to application directory
cd ChecklistGenerator || exit 1

# Clean previous builds
echo "🧹 Cleaning previous builds..."
dotnet clean

# Restore and build
echo "📦 Restoring packages..."
dotnet restore

echo "🔨 Building application..."
dotnet build --configuration Release

# Check if build was successful
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    echo ""
    echo "🚀 Starting application..."
    echo "📝 Application will be available on port 5000"
    echo "🌐 Use the Ports tab to access the forwarded URL"
    echo ""
    
    # Start the application
    dotnet run --urls "http://0.0.0.0:5000" --configuration Release
else
    echo "❌ Build failed!"
    echo "🔧 Check the build output above for errors"
    exit 1
fi
