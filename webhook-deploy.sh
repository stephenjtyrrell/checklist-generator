#!/bin/bash

# GitHub Webhook Handler for Codespace Auto-Deployment
# This script can be called from a webhook to auto-update Codespaces

echo "🎯 GitHub Webhook Deployment Handler"
echo "===================================="

# Get the latest commit info
COMMIT_SHA=$(git rev-parse HEAD)
BRANCH=$(git branch --show-current)

echo "📊 Current Status:"
echo "   Branch: $BRANCH"
echo "   Commit: $COMMIT_SHA"
echo "   Time: $(date)"

# Check for changes
echo "🔍 Checking for updates..."
git fetch origin

LOCAL_COMMIT=$(git rev-parse HEAD)
REMOTE_COMMIT=$(git rev-parse origin/$BRANCH)

if [ "$LOCAL_COMMIT" = "$REMOTE_COMMIT" ]; then
    echo "✅ Already up to date"
    exit 0
fi

echo "📥 New changes detected, updating..."

# Pull changes and restart
./codespace-update.sh

echo "🎉 Deployment complete!"
