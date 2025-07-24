# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app
USER appuser

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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChecklistGenerator.dll"]
