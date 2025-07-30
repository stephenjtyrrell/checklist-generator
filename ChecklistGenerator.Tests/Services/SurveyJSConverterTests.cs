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
        private readonly Mock<IAzureAIFoundryService> _mockAzureAIFoundryService;
        private readonly SurveyJSConverter _converter;

        public SurveyJSConverterTests()
        {
            _mockAzureAIFoundryService = new Mock<IAzureAIFoundryService>();
            
            _converter = new SurveyJSConverter(_mockAzureAIFoundryService.Object, new NullLogger<SurveyJSConverter>());
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

            _mockAzureAIFoundryService
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
        public async Task ConvertToSurveyJSAsync_WithAzureAIResponse_ShouldReturnAIGeneratedSurvey()
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

            _mockAzureAIFoundryService
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
        public async Task ConvertToSurveyJSAsync_AzureAIServiceFails_ShouldThrowException()
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

            _mockAzureAIFoundryService
                .Setup(x => x.ConvertChecklistToSurveyJSAsync(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .ReturnsAsync(string.Empty); // Simulate failure

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey"));
            
            exception.Message.Should().Contain("Azure AI Foundry service returned empty or null response");
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_AzureAIServiceThrowsException_ShouldPropagateException()
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

            var expectedExceptionMessage = "AI service connection failed";
            _mockAzureAIFoundryService
                .Setup(x => x.ConvertChecklistToSurveyJSAsync(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException(expectedExceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey"));
            
            exception.Message.Should().Contain(expectedExceptionMessage);
        }
    }
}
