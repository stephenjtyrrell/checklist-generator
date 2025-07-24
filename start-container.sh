#!/bin/bash

# Create log directory
mkdir -p /var/log/supervisor

# Primary domain for SSL certificate (custom domain)
PRIMARY_DOMAIN="checklist.stephentyrrell.ie"
AZURE_FQDN="checklist-generator-stable.eastus.azurecontainer.io"

echo "Generating SSL certificate for custom domain: $PRIMARY_DOMAIN"

# Generate SSL certificate for the custom domain (primary)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/ssl/private/checklist.key \
    -out /etc/ssl/certs/checklist.crt \
    -subj "/C=IE/ST=Dublin/L=Dublin/O=ChecklistGenerator/OU=IT/CN=$PRIMARY_DOMAIN" \
    -addext "subjectAltName=DNS:$PRIMARY_DOMAIN,DNS:$AZURE_FQDN,DNS:localhost,IP:127.0.0.1"

echo "SSL certificate generated successfully!"
echo "  Primary domain: $PRIMARY_DOMAIN" 
echo "  Azure FQDN: $AZURE_FQDN"
echo "  SAN includes: checklist.stephentyrrell.ie, checklist-generator-stable.eastus.azurecontainer.io, localhost"

# Start supervisor which will manage both nginx and the .NET app
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
