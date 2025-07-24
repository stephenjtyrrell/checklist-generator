using ChecklistGenerator.Controllers;
using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace ChecklistGenerator.Tests.Controllers
{
    public class ChecklistControllerTests
    {
        private readonly ChecklistController _controller;
        private readonly Mock<DocxToExcelConverter> _mockDocxConverter;
        private readonly Mock<ExcelProcessor> _mockExcelProcessor;
        private readonly Mock<SurveyJSConverter> _mockSurveyConverter;
        private readonly Mock<ILogger<ChecklistController>> _mockLogger;

        public ChecklistControllerTests()
        {
            _mockDocxConverter = new Mock<DocxToExcelConverter>(Mock.Of<ILogger<DocxToExcelConverter>>());
            _mockExcelProcessor = new Mock<ExcelProcessor>(Mock.Of<ILogger<ExcelProcessor>>());
            _mockSurveyConverter = new Mock<SurveyJSConverter>();
            _mockLogger = new Mock<ILogger<ChecklistController>>();

            _controller = new ChecklistController(
                _mockDocxConverter.Object,
                _mockExcelProcessor.Object,
                _mockSurveyConverter.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task UploadAndConvert_NoFile_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.UploadAndConvert(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("No file uploaded");
        }

        [Fact]
        public async Task UploadAndConvert_EmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFile("test.docx", 0);

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("No file uploaded");
        }

        [Fact]
        public async Task UploadAndConvert_NonDocxFile_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFile("test.txt", 100);

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Only .docx files are supported");
        }

        [Fact]
        public async Task UploadAndConvert_ValidFile_ShouldReturnOkResult()
        {
            // Arrange
            var mockFile = CreateMockFile("test.docx", 1000);
            var excelStream = new MemoryStream();
            var excelBytes = new byte[] { 1, 2, 3 };
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem { Id = "item1", Text = "Test item", Type = ChecklistItemType.Boolean }
            };
            var surveyJson = "{ \"test\": \"survey\" }";

            _mockDocxConverter.Setup(x => x.ConvertDocxToExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((excelStream, excelBytes, "test.xlsx"));

            _mockExcelProcessor.Setup(x => x.ProcessExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(checklistItems);

            _mockSurveyConverter.Setup(x => x.ConvertToSurveyJS(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .Returns(surveyJson);

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
            
            // Check that the response contains expected properties
            var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"fileName\":\"test.docx\"");
            responseJson.Should().Contain("\"itemCount\":1");
        }

        [Fact]
        public async Task UploadAndConvert_ConversionFails_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFile("test.docx", 1000);

            _mockDocxConverter.Setup(x => x.ConvertDocxToExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Conversion failed"));

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.ToString().Should().Contain("Failed to convert DOCX to Excel");
        }

        [Fact]
        public async Task UploadAndConvert_ExcelProcessingFails_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = CreateMockFile("test.docx", 1000);
            var excelStream = new MemoryStream();
            var excelBytes = new byte[] { 1, 2, 3 };

            _mockDocxConverter.Setup(x => x.ConvertDocxToExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((excelStream, excelBytes, "test.xlsx"));

            _mockExcelProcessor.Setup(x => x.ProcessExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Processing failed"));

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.ToString().Should().Contain("Failed to process Excel file");
        }

        [Fact]
        public async Task UploadAndConvert_NoItemsFound_ShouldCreateDefaultItem()
        {
            // Arrange
            var mockFile = CreateMockFile("test.docx", 1000);
            var excelStream = new MemoryStream();
            var excelBytes = new byte[] { 1, 2, 3 };
            var emptyChecklistItems = new List<ChecklistItem>();
            var surveyJson = "{ \"test\": \"survey\" }";

            _mockDocxConverter.Setup(x => x.ConvertDocxToExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((excelStream, excelBytes, "test.xlsx"));

            _mockExcelProcessor.Setup(x => x.ProcessExcelAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(emptyChecklistItems);

            _mockSurveyConverter.Setup(x => x.ConvertToSurveyJS(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .Returns(surveyJson);

            // Act
            var result = await _controller.UploadAndConvert(mockFile);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            // Verify that SurveyConverter was called with at least one item (the default "no items found" item)
            _mockSurveyConverter.Verify(x => x.ConvertToSurveyJS(
                It.Is<List<ChecklistItem>>(items => items.Count > 0 && 
                    items.Any(item => item.Id == "no_items_found")), 
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void GetSampleSurvey_ShouldReturnSampleData()
        {
            // Arrange
            var expectedSurveyJson = "{ \"sample\": \"survey\" }";
            _mockSurveyConverter.Setup(x => x.ConvertToSurveyJS(It.IsAny<List<ChecklistItem>>(), It.IsAny<string>()))
                .Returns(expectedSurveyJson);

            // Act
            var result = _controller.GetSampleSurvey();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
            responseJson.Should().Contain("\"success\":true");

            // Verify sample data was created correctly
            _mockSurveyConverter.Verify(x => x.ConvertToSurveyJS(
                It.Is<List<ChecklistItem>>(items => 
                    items.Count == 3 &&
                    items.Any(item => item.Id == "sample_1" && item.Type == ChecklistItemType.Text) &&
                    items.Any(item => item.Id == "sample_2" && item.Type == ChecklistItemType.Boolean) &&
                    items.Any(item => item.Id == "sample_3" && item.Type == ChecklistItemType.RadioGroup)),
                "Sample Survey"), Times.Once);
        }

        [Fact]
        public void SaveSurveyResults_ValidData_ShouldReturnSuccess()
        {
            // Arrange
            var request = new SaveSurveyResultsRequest
            {
                SurveyData = new Dictionary<string, object>
                {
                    { "question1", "answer1" },
                    { "question2", true }
                },
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            // Act
            var result = _controller.SaveSurveyResults(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var responseJson = System.Text.Json.JsonSerializer.Serialize(okResult!.Value);
            responseJson.Should().Contain("\"success\":true");
            responseJson.Should().Contain("\"message\":\"Survey results saved successfully\"");
        }

        [Fact]
        public void SaveSurveyResults_NullRequest_ShouldReturnBadRequest()
        {
            // Act
            var result = _controller.SaveSurveyResults(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("No survey data provided");
        }

        [Fact]
        public void SaveSurveyResults_NullSurveyData_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new SaveSurveyResultsRequest { SurveyData = null! };

            // Act
            var result = _controller.SaveSurveyResults(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("No survey data provided");
        }

        [Fact]
        public void DownloadExcel_InvalidId_ShouldReturnNotFound()
        {
            // Act
            var result = _controller.DownloadExcel("invalid-id");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Excel file not found or has expired");
        }

        private IFormFile CreateMockFile(string fileName, long length)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.FileName).Returns(fileName);
            mock.Setup(f => f.Length).Returns(length);
            
            if (length > 0)
            {
                var content = Encoding.UTF8.GetBytes("test content");
                var stream = new MemoryStream(content);
                mock.Setup(f => f.OpenReadStream()).Returns(stream);
            }
            
            return mock.Object;
        }
    }
}
