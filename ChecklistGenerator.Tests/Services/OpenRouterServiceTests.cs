using ChecklistGenerator.Services;
using ChecklistGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace ChecklistGenerator.Tests.Services
{
    public class OpenRouterServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly OpenRouterService _openRouterService;

        public OpenRouterServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            
            _configurationMock.Setup(x => x["OpenRouterApiKey"]).Returns("test-api-key");

            _openRouterService = new OpenRouterService(_httpClient, _configurationMock.Object, new NullLogger<OpenRouterService>());
        }

        [Fact]
        public async Task ConvertDocumentToChecklistAsync_WithValidResponse_ShouldReturnChecklistItems()
        {
            // Arrange
            var documentContent = "1. Complete registration form\n2. Submit required documents\n3. Pay application fee";
            var fileName = "test-document.docx";

            // Create the expected JSON structure that matches OpenRouter API response format
            var openRouterResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(new[]
                            {
                                new
                                {
                                    id = "item_1",
                                    text = "Complete registration form",
                                    description = "Fill out all required fields in the registration form",
                                    type = "Checkbox",
                                    isRequired = true,
                                    options = new string[0]
                                },
                                new
                                {
                                    id = "item_2",
                                    text = "Submit required documents",
                                    description = "Provide all necessary documentation",
                                    type = "Checkbox",
                                    isRequired = true,
                                    options = new string[0]
                                },
                                new
                                {
                                    id = "item_3",
                                    text = "Pay application fee",
                                    description = "Complete payment processing",
                                    type = "Checkbox",
                                    isRequired = true,
                                    options = new string[0]
                                }
                            })
                        }
                    }
                }
            };

            var responseContent = JsonSerializer.Serialize(openRouterResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.ConvertDocumentToChecklistAsync(documentContent, fileName);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result[0].Id.Should().Be("item_1");
            result[0].Text.Should().Be("Complete registration form");
            result[0].Type.Should().Be(ChecklistItemType.Checkbox);
            result[0].IsRequired.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithoutApiKey_ShouldThrowArgumentException()
        {
            // Arrange
            var mockConfigWithoutKey = new Mock<IConfiguration>();
            mockConfigWithoutKey.Setup(x => x["OpenRouterApiKey"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new OpenRouterService(_httpClient, mockConfigWithoutKey.Object, new NullLogger<OpenRouterService>()));
        }

        [Fact]
        public async Task ConvertDocumentToChecklistAsync_WithApiError_ShouldReturnErrorItem()
        {
            // Arrange
            var documentContent = "test content";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("API Error", Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.ConvertDocumentToChecklistAsync(documentContent);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Id.Should().Be("openrouter_conversion_error");
            result[0].Type.Should().Be(ChecklistItemType.Comment);
            result[0].Text.Should().Contain("Failed to process document with AI");
        }

        [Fact]
        public async Task ConvertChecklistToSurveyJSAsync_WithValidItems_ShouldReturnSurveyJS()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item_1",
                    Text = "Test item 1",
                    Type = ChecklistItemType.Boolean,
                    IsRequired = true
                }
            };

            var surveyJSResponse = new
            {
                title = "Test Survey",
                description = "Generated survey",
                showProgressBar = "top",
                completeText = "Submit",
                showQuestionNumbers = "off",
                questionTitleLocation = "top",
                pages = new[]
                {
                    new
                    {
                        name = "page1",
                        title = "Page 1",
                        elements = new[]
                        {
                            new
                            {
                                type = "boolean",
                                name = "item_1",
                                title = "Test item 1",
                                isRequired = true
                            }
                        }
                    }
                }
            };

            var openRouterResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(surveyJSResponse)
                        }
                    }
                }
            };

            var responseContent = JsonSerializer.Serialize(openRouterResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.ConvertChecklistToSurveyJSAsync(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            var parsedResult = JsonSerializer.Deserialize<JsonElement>(result);
            parsedResult.GetProperty("title").GetString().Should().Be("Test Survey");
            parsedResult.GetProperty("pages").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task ConvertChecklistToSurveyJSAsync_WithApiError_ShouldReturnFallbackSurvey()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();
            var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.ConvertChecklistToSurveyJSAsync(checklistItems, "Empty Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            var parsedResult = JsonSerializer.Deserialize<JsonElement>(result);
            parsedResult.GetProperty("title").GetString().Should().Be("Empty Survey");
            parsedResult.GetProperty("description").GetString().Should().Be("Survey generation failed");
        }

        [Fact]
        public async Task EnhanceChecklistAsync_WithValidItems_ShouldReturnEnhancedItems()
        {
            // Arrange
            var existingItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item_1",
                    Text = "Basic item",
                    Type = ChecklistItemType.Text
                }
            };

            var enhancedItems = new[]
            {
                new
                {
                    id = "item_1",
                    text = "Enhanced basic item with more details",
                    description = "Additional context",
                    type = "Text",
                    isRequired = false,
                    options = new string[0]
                }
            };

            var openRouterResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = JsonSerializer.Serialize(enhancedItems)
                        }
                    }
                }
            };

            var responseContent = JsonSerializer.Serialize(openRouterResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.EnhanceChecklistAsync(existingItems);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Text.Should().Be("Enhanced basic item with more details");
            result[0].Description.Should().Be("Additional context");
        }

        [Fact]
        public async Task EnhanceChecklistAsync_WithApiError_ShouldReturnOriginalItems()
        {
            // Arrange
            var existingItems = new List<ChecklistItem>
            {
                new ChecklistItem { Id = "item_1", Text = "Original item" }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _openRouterService.EnhanceChecklistAsync(existingItems);

            // Assert
            result.Should().BeSameAs(existingItems);
        }
    }
}
