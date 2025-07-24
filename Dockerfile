# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install nginx and supervisor
USER root
RUN apt-get update && apt-get install -y \
    nginx \
    supervisor \
    openssl \
    && rm -rf /var/lib/apt/lists/*

# Create SSL directories (certificates will be generated at runtime)
RUN mkdir -p /etc/ssl/private /etc/ssl/certs

# Expose standard web ports (80 for HTTP, 443 for HTTPS)
EXPOSE 80
EXPOSE 443

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ChecklistGenerator/ChecklistGenerator.csproj", "ChecklistGenerator/"]
RUN dotnet restore "ChecklistGenerator/ChecklistGenerator.csproj"
COPY . .
WORKDIR "/src/ChecklistGenerator"
RUN dotnet build "ChecklistGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChecklistGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Create startup script
COPY start-container.sh /start-container.sh
RUN chmod +x /start-container.sh

# Start both nginx and the .NET application
ENTRYPOINT ["/start-container.sh"]
