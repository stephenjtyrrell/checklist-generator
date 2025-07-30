using ChecklistGenerator.Services;
using ChecklistGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class AzureDocumentIntelligenceServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AzureDocumentIntelligenceService>> _mockLogger;

        public AzureDocumentIntelligenceServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AzureDocumentIntelligenceService>>();
        }

        [Fact]
        public void Constructor_ThrowsArgumentException_WhenEndpointNotConfigured()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:Endpoint"]).Returns((string?)null);
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:ApiKey"]).Returns("test-key");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new AzureDocumentIntelligenceService(_mockConfiguration.Object, _mockLogger.Object));
            
            Assert.Contains("AzureDocumentIntelligence:Endpoint not configured", exception.Message);
        }

        [Fact]
        public void Constructor_ThrowsArgumentException_WhenApiKeyNotConfigured()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:Endpoint"]).Returns("https://test.cognitiveservices.azure.com/");
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:ApiKey"]).Returns((string?)null);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new AzureDocumentIntelligenceService(_mockConfiguration.Object, _mockLogger.Object));
            
            Assert.Contains("AzureDocumentIntelligence:ApiKey not configured", exception.Message);
        }

        [Fact]
        public async Task ProcessDocumentAsync_ThrowsArgumentNullException_WhenStreamIsNull()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:Endpoint"]).Returns("https://test.cognitiveservices.azure.com/");
            _mockConfiguration.Setup(c => c["AzureDocumentIntelligence:ApiKey"]).Returns("test-key");

            var service = new AzureDocumentIntelligenceService(_mockConfiguration.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.ProcessDocumentAsync(null!, "test.docx"));
        }
    }
}
