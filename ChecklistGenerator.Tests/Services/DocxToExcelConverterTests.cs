using ChecklistGenerator.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class DocxToExcelConverterTests : IDisposable
    {
        private readonly DocxToExcelConverter _converter;
        private readonly Mock<ILogger<DocxToExcelConverter>> _mockLogger;
        private readonly List<string> _tempFiles = new();

        public DocxToExcelConverterTests()
        {
            _mockLogger = new Mock<ILogger<DocxToExcelConverter>>();
            _converter = new DocxToExcelConverter(_mockLogger.Object);
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_ValidDocx_ShouldReturnExcelData()
        {
            // Arrange
            var docxStream = CreateTestDocxWithTable();
            var fileName = "test.docx";

            // Act
            var result = await _converter.ConvertDocxToExcelAsync(docxStream, fileName);

            // Assert
            result.ExcelStream.Should().NotBeNull();
            result.ExcelBytes.Should().NotBeNull().And.HaveCountGreaterThan(0);
            result.FileName.Should().EndWith(".xlsx").And.Contain("test");
            
            // Verify Excel content
            result.ExcelStream.Position = 0;
            using var workbook = new XSSFWorkbook(result.ExcelStream);
            var sheet = workbook.GetSheetAt(0);
            sheet.Should().NotBeNull();
            sheet.LastRowNum.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_EmptyDocx_ShouldReturnValidExcel()
        {
            // Arrange
            var docxStream = CreateEmptyDocx();
            var fileName = "empty.docx";

            // Act
            var result = await _converter.ConvertDocxToExcelAsync(docxStream, fileName);

            // Assert
            result.ExcelStream.Should().NotBeNull();
            result.ExcelBytes.Should().NotBeNull().And.HaveCountGreaterThan(0);
            result.FileName.Should().EndWith(".xlsx");
            
            // Verify Excel content has default structure
            result.ExcelStream.Position = 0;
            using var workbook = new XSSFWorkbook(result.ExcelStream);
            var sheet = workbook.GetSheetAt(0);
            sheet.Should().NotBeNull();
            
            // Should have at least header and one row
            sheet.LastRowNum.Should().BeGreaterThanOrEqualTo(1);
            var headerCell = sheet.GetRow(0)?.GetCell(0);
            headerCell?.ToString().Should().Be("Content");
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_DocxWithParagraphs_ShouldExtractText()
        {
            // Arrange
            var docxStream = CreateTestDocxWithParagraphs();
            var fileName = "paragraphs.docx";

            // Act
            var result = await _converter.ConvertDocxToExcelAsync(docxStream, fileName);

            // Assert
            result.ExcelStream.Position = 0;
            using var workbook = new XSSFWorkbook(result.ExcelStream);
            var sheet = workbook.GetSheetAt(0);
            
            // Verify content extraction
            var headerCell = sheet.GetRow(0)?.GetCell(0);
            headerCell?.ToString().Should().Be("Content");
            
            var contentCell = sheet.GetRow(1)?.GetCell(0);
            contentCell?.ToString().Should().Contain("Test paragraph");
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_InvalidStream_ShouldThrowException()
        {
            // Arrange
            var invalidStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var fileName = "invalid.docx";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _converter.ConvertDocxToExcelAsync(invalidStream, fileName));
            
            exception.Message.Should().Contain("Failed to convert DOCX to Excel");
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_FileNameWithSpecialCharacters_ShouldSanitizeFileName()
        {
            // Arrange
            var docxStream = CreateEmptyDocx();
            var fileName = "test<file>name?.docx";

            // Act
            var result = await _converter.ConvertDocxToExcelAsync(docxStream, fileName);

            // Assert
            result.FileName.Should().NotContain("<").And.NotContain(">").And.NotContain("?");
            result.FileName.Should().EndWith(".xlsx");
        }

        [Fact]
        public async Task ConvertDocxToExcelAsync_ShouldIncludeTimestamp()
        {
            // Arrange
            var docxStream = CreateEmptyDocx();
            var fileName = "test.docx";

            // Act
            var result = await _converter.ConvertDocxToExcelAsync(docxStream, fileName);

            // Assert
            result.FileName.Should().MatchRegex(@"test_\d{8}_\d{6}\.xlsx");
        }

        private MemoryStream CreateTestDocxWithTable()
        {
            var stream = new MemoryStream();
            
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Create a table
                var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
                
                // Add table properties
                var tblPr = new TableProperties();
                var tblW = new TableWidth() { Width = "0", Type = TableWidthUnitValues.Auto };
                tblPr.Append(tblW);
                table.AppendChild(tblPr);

                // Add header row
                var headerRow = new TableRow();
                headerRow.Append(CreateTableCell("Header 1"));
                headerRow.Append(CreateTableCell("Header 2"));
                table.Append(headerRow);

                // Add data row
                var dataRow = new TableRow();
                dataRow.Append(CreateTableCell("Data 1"));
                dataRow.Append(CreateTableCell("Data 2"));
                table.Append(dataRow);

                body.Append(table);
            }

            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateTestDocxWithParagraphs()
        {
            var stream = new MemoryStream();
            
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Add paragraphs
                var para1 = new Paragraph();
                var run1 = new Run();
                run1.Append(new Text("Test paragraph 1"));
                para1.Append(run1);
                body.Append(para1);

                var para2 = new Paragraph();
                var run2 = new Run();
                run2.Append(new Text("Test paragraph 2"));
                para2.Append(run2);
                body.Append(para2);
            }

            stream.Position = 0;
            return stream;
        }

        private MemoryStream CreateEmptyDocx()
        {
            var stream = new MemoryStream();
            
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());
            }

            stream.Position = 0;
            return stream;
        }

        private TableCell CreateTableCell(string text)
        {
            var cell = new TableCell();
            var paragraph = new Paragraph();
            var run = new Run();
            run.Append(new Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);
            return cell;
        }

        public void Dispose()
        {
            foreach (var tempFile in _tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
