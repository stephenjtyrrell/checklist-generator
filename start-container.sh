#!/bin/bash

# Create log directory
mkdir -p /var/log/supervisor

# Get the container's hostname/FQDN
HOSTNAME=$(hostname -f)
if [ -z "$HOSTNAME" ] || [ "$HOSTNAME" = "localhost" ]; then
    HOSTNAME="checklist-generator-stable.eastus.azurecontainer.io"
fi

echo "Generating SSL certificate for: $HOSTNAME"

# Generate SSL certificate dynamically based on current hostname
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/ssl/private/checklist.key \
    -out /etc/ssl/certs/checklist.crt \
    -subj "/C=IE/ST=Dublin/L=Dublin/O=ChecklistGenerator/OU=IT/CN=$HOSTNAME" \
    -addext "subjectAltName=DNS:$HOSTNAME,DNS:checklist.stephentyrrell.ie,DNS:localhost,IP:127.0.0.1"

echo "SSL certificate generated for: $HOSTNAME"

# Start supervisor which will manage both nginx and the .NET app
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
