using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class ExcelProcessorTests
    {
        private readonly ExcelProcessor _processor;
        private readonly Mock<ILogger<ExcelProcessor>> _mockLogger;

        public ExcelProcessorTests()
        {
            _mockLogger = new Mock<ILogger<ExcelProcessor>>();
            _processor = new ExcelProcessor(_mockLogger.Object);
        }

        [Fact]
        public async Task ProcessExcelAsync_ValidExcelWithHeaders_ShouldReturnChecklistItems()
        {
            // Arrange
            var excelStream = CreateTestExcelWithHeaders();

            // Act
            var result = await _processor.ProcessExcelAsync(excelStream, "test.xlsx");

            // Assert
            result.Should().NotBeNull().And.HaveCountGreaterThan(0);
            result.All(item => !string.IsNullOrEmpty(item.Id)).Should().BeTrue();
            result.All(item => !string.IsNullOrEmpty(item.Text)).Should().BeTrue();
        }

        [Fact]
        public async Task ProcessExcelAsync_EmptyExcel_ShouldThrowException()
        {
            // Arrange
            var excelStream = CreateEmptyExcel();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _processor.ProcessExcelAsync(excelStream, "empty.xlsx"));
            
            exception.Message.Should().Contain("No header row found");
        }

        [Fact]
        public async Task ProcessExcelAsync_ExcelWithOnlyHeaders_ShouldReturnGenericItems()
        {
            // Arrange
            var excelStream = CreateExcelWithOnlyHeaders();

            // Act
            var result = await _processor.ProcessExcelAsync(excelStream, "headers-only.xlsx");

            // Assert
            result.Should().NotBeNull().And.HaveCountGreaterThan(0);
            // Should create generic items when no data rows found
        }

        [Fact]
        public async Task ProcessExcelAsync_InvalidExcelFile_ShouldReturnErrorItem()
        {
            // Arrange
            var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            // Act
            var result = await _processor.ProcessExcelAsync(invalidStream, "invalid.xlsx");

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be("excel_processing_error");
            result[0].Text.Should().Be("Failed to process Excel content");
            result[0].Type.Should().Be(ChecklistItemType.Comment);
        }

        [Fact]
        public async Task ProcessExcelAsync_ExcelWithMixedContent_ShouldProcessAllRows()
        {
            // Arrange
            var excelStream = CreateExcelWithMixedContent();

            // Act
            var result = await _processor.ProcessExcelAsync(excelStream, "mixed.xlsx");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
            
            // Verify that items have appropriate types
            result.Should().Contain(item => item.Type == ChecklistItemType.Boolean);
        }

        [Fact]
        public async Task ProcessExcelAsync_ExcelWithEmptyRows_ShouldSkipEmptyRows()
        {
            // Arrange
            var excelStream = CreateExcelWithEmptyRows();

            // Act
            var result = await _processor.ProcessExcelAsync(excelStream, "empty-rows.xlsx");

            // Assert
            result.Should().NotBeNull();
            // Should not include items for completely empty rows
            result.All(item => !string.IsNullOrWhiteSpace(item.Text)).Should().BeTrue();
        }

        [Fact]
        public async Task ProcessExcelAsync_NullStream_ShouldReturnErrorItem()
        {
            // Arrange
            Stream nullStream = null!;

            // Act
            var result = await _processor.ProcessExcelAsync(nullStream, "null.xlsx");

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be("excel_processing_error");
            result[0].Type.Should().Be(ChecklistItemType.Comment);
        }

        [Theory]
        [InlineData("Question")]
        [InlineData("Task")]
        [InlineData("Item")]
        [InlineData("Requirement")]
        [InlineData("Check")]
        public async Task ProcessExcelAsync_HeadersWithKeywords_ShouldBeRecognized(string headerKeyword)
        {
            // Arrange
            var excelStream = CreateExcelWithSpecificHeader(headerKeyword);

            // Act
            var result = await _processor.ProcessExcelAsync(excelStream, "test.xlsx");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
        }

        private MemoryStream CreateTestExcelWithHeaders()
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create header row
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("Question");
            headerRow.CreateCell(1).SetCellValue("Type");
            headerRow.CreateCell(2).SetCellValue("Required");

            // Create data rows
            var row1 = sheet.CreateRow(1);
            row1.CreateCell(0).SetCellValue("Is the project approved?");
            row1.CreateCell(1).SetCellValue("Yes/No");
            row1.CreateCell(2).SetCellValue("Required");

            var row2 = sheet.CreateRow(2);
            row2.CreateCell(0).SetCellValue("Enter project name");
            row2.CreateCell(1).SetCellValue("Text");
            row2.CreateCell(2).SetCellValue("Optional");

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateEmptyExcel()
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");
            // No rows created - completely empty

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateExcelWithOnlyHeaders()
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create only header row
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("Question");
            headerRow.CreateCell(1).SetCellValue("Type");

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateExcelWithMixedContent()
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create header row
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("Content");

            // Create data rows with different types of content
            var row1 = sheet.CreateRow(1);
            row1.CreateCell(0).SetCellValue("1. First checklist item");

            var row2 = sheet.CreateRow(2);
            row2.CreateCell(0).SetCellValue("☐ Checkbox item");

            var row3 = sheet.CreateRow(3);
            row3.CreateCell(0).SetCellValue("Regular text content");

            var row4 = sheet.CreateRow(4);
            row4.CreateCell(0).SetCellValue("□ Another checkbox");

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateExcelWithEmptyRows()
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create header row
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("Question");

            // Create data row
            var row1 = sheet.CreateRow(1);
            row1.CreateCell(0).SetCellValue("Valid question");

            // Create empty row (row 2 exists but has no content)
            sheet.CreateRow(2);

            // Create another data row
            var row3 = sheet.CreateRow(3);
            row3.CreateCell(0).SetCellValue("Another valid question");

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateExcelWithSpecificHeader(string headerName)
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create header row with specific header
            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue(headerName);

            // Create data row
            var row1 = sheet.CreateRow(1);
            row1.CreateCell(0).SetCellValue("Sample content");

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }
    }
}
