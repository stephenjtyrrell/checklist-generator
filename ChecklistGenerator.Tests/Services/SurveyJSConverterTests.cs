using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class SurveyJSConverterTests
    {
        private readonly Mock<GeminiService> _mockGeminiService;
        private readonly SurveyJSConverter _converter;

        public SurveyJSConverterTests()
        {
            var mockHttpClient = Mock.Of<HttpClient>();
            var mockConfiguration = new Mock<IConfiguration>();
            
            // Setup a valid API key for the mock configuration
            mockConfiguration.Setup(x => x["GeminiApiKey"]).Returns("test-api-key");
            
            _mockGeminiService = new Mock<GeminiService>(
                mockHttpClient,
                mockConfiguration.Object,
                new NullLogger<GeminiService>());
            
            _converter = new SurveyJSConverter(_mockGeminiService.Object, new NullLogger<SurveyJSConverter>());
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_EmptyList_ShouldReturnValidJson()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();
            var expectedSurveyJS = """
            {
                "title": "Test Survey",
                "description": "Generated from an Excel document",
                "pages": []
            }
            """;

            _mockGeminiService
                .Setup(x => x.ConvertChecklistToSurveyJSAsync(checklistItems, "Test Survey"))
                .ReturnsAsync(expectedSurveyJS);

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Test Survey");
            jsonDocument.RootElement.GetProperty("description").GetString().Should().Contain("Generated from an Excel document");
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_WithGeminiResponse_ShouldReturnAIGeneratedSurvey()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test question 1",
                    Type = ChecklistItemType.Boolean,
                    IsRequired = true
                }
            };

            var expectedSurveyJS = """
            {
                "title": "Test Survey",
                "pages": [
                    {
                        "name": "page1",
                        "elements": [
                            {
                                "type": "boolean",
                                "name": "item1",
                                "title": "Test question 1",
                                "isRequired": true
                            }
                        ]
                    }
                ]
            }
            """;

            _mockGeminiService
                .Setup(x => x.ConvertChecklistToSurveyJSAsync(checklistItems, "Test Survey"))
                .ReturnsAsync(expectedSurveyJS);

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Test Survey");
            jsonDocument.RootElement.GetProperty("pages").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_GeminiServiceFails_ShouldReturnFallbackSurvey()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test question",
                    Type = ChecklistItemType.Boolean
                }
            };

            _mockGeminiService
                .Setup(x => x.ConvertChecklistToSurveyJSAsync(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .ReturnsAsync(string.Empty); // Simulate failure

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Fallback Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Fallback Survey");
            // Should contain fallback message in description
            jsonDocument.RootElement.GetProperty("description").GetString().Should().Contain("AI service unavailable");
        }
    }
}
