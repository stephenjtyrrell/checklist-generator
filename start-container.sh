#!/bin/bash

# Create log directory
mkdir -p /var/log/supervisor

# Start supervisor which will manage both nginx and the .NET app
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
