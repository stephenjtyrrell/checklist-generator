using ChecklistGenerator.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Integration
{
    public class ChecklistControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ChecklistControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override logging to reduce noise in tests
                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Warning);
                    });
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetSampleSurvey_ShouldReturnValidSurveyJson()
        {
            // Act
            var response = await _client.GetAsync("/api/checklist/sample");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            jsonDoc.RootElement.TryGetProperty("surveyJS", out var surveyJS).Should().BeTrue();
            
            // Verify SurveyJS is valid JSON
            var surveyContent = surveyJS.GetString();
            surveyContent.Should().NotBeNullOrEmpty();
            
            var surveyDoc = JsonDocument.Parse(surveyContent!);
            surveyDoc.RootElement.GetProperty("title").GetString().Should().Be("Sample Survey");
        }

        [Fact]
        public async Task SaveSurveyResults_ValidData_ShouldReturnSuccess()
        {
            // Arrange
            var requestData = new
            {
                SurveyData = new Dictionary<string, object>
                {
                    { "sample_1", "Test Company" },
                    { "sample_2", true },
                    { "sample_3", "UCITS" }
                },
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/checklist/saveResults", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("Survey results saved successfully");
            jsonDoc.RootElement.TryGetProperty("id", out var idElement).Should().BeTrue();
            idElement.GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SaveSurveyResults_EmptyData_ShouldReturnBadRequest()
        {
            // Arrange
            var content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/checklist/saveResults", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UploadAndConvert_NoFile_ShouldReturnBadRequest()
        {
            // Arrange
            var formContent = new MultipartFormDataContent();

            // Act
            var response = await _client.PostAsync("/api/checklist/upload", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UploadAndConvert_NonDocxFile_ShouldReturnBadRequest()
        {
            // Arrange
            var formContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            formContent.Add(fileContent, "file", "test.txt");

            // Act
            var response = await _client.PostAsync("/api/checklist/upload", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Only .docx files are supported");
        }

        [Fact]
        public async Task DownloadExcel_InvalidId_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/checklist/downloadExcel/invalid-id");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RootEndpoint_ShouldRedirectToIndex()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/index.html");
        }

        [Fact]
        public async Task StaticFiles_ShouldBeServed()
        {
            // Act
            var response = await _client.GetAsync("/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        }

        [Theory]
        [InlineData("/api/checklist/sample", "GET")]
        [InlineData("/api/checklist/saveResults", "POST")]
        [InlineData("/api/checklist/upload", "POST")]
        public async Task Endpoints_ShouldSupportCors(string endpoint, string method)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
            request.Headers.Add("Origin", "http://localhost:3000");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
        }
    }
}
