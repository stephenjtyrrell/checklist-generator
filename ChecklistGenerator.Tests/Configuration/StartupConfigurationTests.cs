using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChecklistGenerator.Tests.Configuration
{
    public class StartupConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public StartupConfigurationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Services_ShouldBeRegisteredCorrectly()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Act & Assert
            serviceProvider.GetService<DocxToExcelConverter>().Should().NotBeNull();
            serviceProvider.GetService<ExcelProcessor>().Should().NotBeNull();
            serviceProvider.GetService<SurveyJSConverter>().Should().NotBeNull();
        }

        [Fact]
        public void Services_ShouldBeScoped()
        {
            // Arrange
            using var scope1 = _factory.Services.CreateScope();
            using var scope2 = _factory.Services.CreateScope();

            // Act
            var service1_scope1 = scope1.ServiceProvider.GetService<DocxToExcelConverter>();
            var service2_scope1 = scope1.ServiceProvider.GetService<DocxToExcelConverter>();
            var service1_scope2 = scope2.ServiceProvider.GetService<DocxToExcelConverter>();

            // Assert
            service1_scope1.Should().BeSameAs(service2_scope1); // Same instance within scope
            service1_scope1.Should().NotBeSameAs(service1_scope2); // Different instances across scopes
        }

        [Fact]
        public async Task Cors_ShouldBeConfigured()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/checklist/sample");

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Fact]
        public async Task StaticFiles_ShouldBeEnabled()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/index.html");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task Controllers_ShouldBeMapped()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/checklist/sample");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task RootPath_ShouldRedirectToIndex()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/index.html");
        }
    }
}
