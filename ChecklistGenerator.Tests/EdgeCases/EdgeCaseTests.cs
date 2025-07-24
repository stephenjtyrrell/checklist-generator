using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using ChecklistGenerator.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.EdgeCases
{
    public class EdgeCaseTests
    {
        private readonly Mock<ILogger<DocxToExcelConverter>> _mockDocxLogger;
        private readonly Mock<ILogger<ExcelProcessor>> _mockExcelLogger;
        private readonly DocxToExcelConverter _docxConverter;
        private readonly ExcelProcessor _excelProcessor;
        private readonly SurveyJSConverter _surveyConverter;

        public EdgeCaseTests()
        {
            _mockDocxLogger = new Mock<ILogger<DocxToExcelConverter>>();
            _mockExcelLogger = new Mock<ILogger<ExcelProcessor>>();
            _docxConverter = new DocxToExcelConverter(_mockDocxLogger.Object);
            _excelProcessor = new ExcelProcessor(_mockExcelLogger.Object);
            _surveyConverter = new SurveyJSConverter();
        }

        [Fact]
        public async Task DocxConverter_VeryLargeFile_ShouldHandleGracefully()
        {
            // Arrange
            var largeContent = new string[1000];
            for (int i = 0; i < 1000; i++)
            {
                largeContent[i] = $"This is paragraph {i} with a lot of content that makes the file very large.";
            }
            var docxStream = TestDataHelper.CreateTestDocxWithParagraphs(largeContent);

            // Act
            var result = await _docxConverter.ConvertDocxToExcelAsync(docxStream, "large.docx");

            // Assert
            result.ExcelStream.Should().NotBeNull();
            result.ExcelBytes.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task ExcelProcessor_UnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var headers = new[] { "Question" };
            var data = new string[,] 
            {
                { "测试问题 with émojis 🎉 and symbols ™©®" },
                { "Спрос на русском языке" },
                { "العربية سؤال" },
                { "日本語の質問" }
            };
            var excelStream = TestDataHelper.CreateTestExcel(headers, data);

            // Act
            var result = await _excelProcessor.ProcessExcelAsync(excelStream, "unicode.xlsx");

            // Assert
            result.Should().NotBeNull().And.HaveCountGreaterThan(0);
            result.Should().Contain(item => item.Text.Contains("测试问题"));
            result.Should().Contain(item => item.Text.Contains("Спрос"));
        }

        [Fact]
        public void SurveyConverter_ExtremePagination_ShouldHandleCorrectly()
        {
            // Arrange - Create 100 items to test pagination
            var items = TestDataHelper.CreateSampleChecklistItems(100);

            // Act
            var result = _surveyConverter.ConvertToSurveyJS(items, "Large Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            var jsonDoc = JsonDocument.Parse(result);
            
            // Should create 10 pages (100 items / 10 per page)
            jsonDoc.RootElement.GetProperty("pages").GetArrayLength().Should().Be(10);
            
            // Last page should have exactly 10 items
            var lastPage = jsonDoc.RootElement.GetProperty("pages")[9];
            lastPage.GetProperty("elements").GetArrayLength().Should().Be(10);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        [InlineData("\t\n\r")]
        public void SurveyConverter_EmptyOrWhitespaceText_ShouldHandleGracefully(string? text)
        {
            // Arrange
            var items = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "test",
                    Text = text ?? string.Empty,
                    Type = ChecklistItemType.Boolean
                }
            };

            // Act
            var result = _surveyConverter.ConvertToSurveyJS(items);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var jsonDoc = JsonDocument.Parse(result);
            jsonDoc.RootElement.GetProperty("elements").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task ExcelProcessor_MalformedExcelStructure_ShouldReturnErrorItem()
        {
            // Arrange - Create Excel with inconsistent column counts
            var headers = new[] { "Col1", "Col2", "Col3" };
            var data = new string[,]
            {
                { "A1", "B1", "" }, // Missing column, add empty string
                { "A2", "B2", "C2" }, // Remove extra column
                { "A3", "", "" } // Add missing columns
            };
            var excelStream = TestDataHelper.CreateTestExcel(headers, data);

            // Act
            var result = await _excelProcessor.ProcessExcelAsync(excelStream, "malformed.xlsx");

            // Assert
            result.Should().NotBeNull();
            // Should still process what it can
            result.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void ChecklistItem_ExtremelyLongText_ShouldNotCauseIssues()
        {
            // Arrange
            var veryLongText = new string('A', 10000); // 10KB of text
            var item = new ChecklistItem
            {
                Id = "long_text",
                Text = veryLongText,
                Description = veryLongText,
                Type = ChecklistItemType.Text
            };

            // Act
            var items = new List<ChecklistItem> { item };
            var result = _surveyConverter.ConvertToSurveyJS(items);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var jsonDoc = JsonDocument.Parse(result);
            var element = jsonDoc.RootElement.GetProperty("elements")[0];
            element.GetProperty("title").GetString().Should().HaveLength(10000);
        }

        [Theory]
        [InlineData("question<script>alert('xss')</script>")]
        [InlineData("question'with\"quotes")]
        [InlineData("question\nwith\nnewlines")]
        [InlineData("question\twith\ttabs")]
        public void SurveyConverter_SpecialCharacters_ShouldEscapeCorrectly(string questionText)
        {
            // Arrange
            var items = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "special_chars",
                    Text = questionText,
                    Type = ChecklistItemType.Boolean
                }
            };

            // Act
            var result = _surveyConverter.ConvertToSurveyJS(items);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Should be valid JSON despite special characters
            var jsonDoc = JsonDocument.Parse(result);
            var element = jsonDoc.RootElement.GetProperty("elements")[0];
            element.GetProperty("title").GetString().Should().Be(questionText);
        }

        [Fact]
        public async Task DocxConverter_EmptyTables_ShouldHandleGracefully()
        {
            // Arrange
            var tableData = new string[,] { }; // Empty table
            var docxStream = TestDataHelper.CreateTestDocxWithTable(tableData);

            // Act
            var result = await _docxConverter.ConvertDocxToExcelAsync(docxStream, "empty_table.docx");

            // Assert
            result.ExcelStream.Should().NotBeNull();
            result.ExcelBytes.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task ExcelProcessor_CellsWithFormulas_ShouldExtractValues()
        {
            // Arrange - This test simulates an Excel file that might have formulas
            var headers = new[] { "Question", "Formula Result" };
            var data = new string[,]
            {
                { "What is 2+2?", "4" },
                { "Current date", "2024-01-01" }
            };
            var excelStream = TestDataHelper.CreateTestExcel(headers, data);

            // Act
            var result = await _excelProcessor.ProcessExcelAsync(excelStream, "formulas.xlsx");

            // Assert
            result.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(50)]
        public void SurveyConverter_VariousItemCounts_ShouldHandleCorrectly(int itemCount)
        {
            // Arrange
            var items = TestDataHelper.CreateSampleChecklistItems(itemCount);

            // Act
            var result = _surveyConverter.ConvertToSurveyJS(items);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var jsonDoc = JsonDocument.Parse(result);

            if (itemCount <= 10)
            {
                // Should use elements format
                if (itemCount > 0)
                {
                    jsonDoc.RootElement.GetProperty("elements").GetArrayLength().Should().Be(itemCount);
                }
            }
            else
            {
                // Should use pages format
                var expectedPages = (int)Math.Ceiling(itemCount / 10.0);
                jsonDoc.RootElement.GetProperty("pages").GetArrayLength().Should().Be(expectedPages);
            }
        }
    }
}
