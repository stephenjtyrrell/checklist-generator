using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class RealDocumentNumberingTests
    {
        private readonly DocxToExcelConverter _docxConverter;
        private readonly ExcelProcessor _excelProcessor;
        private readonly SurveyJSConverter _surveyJSConverter;

        public RealDocumentNumberingTests()
        {
            var docxLogger = new NullLogger<DocxToExcelConverter>();
            var excelLogger = new NullLogger<ExcelProcessor>();
            
            _docxConverter = new DocxToExcelConverter(docxLogger);
            _excelProcessor = new ExcelProcessor(excelLogger);
            _surveyJSConverter = new SurveyJSConverter();
        }

        [Fact]
        public async Task RealDocument_FullPipeline_ShouldPreserveComplexNumbering()
        {
            // Arrange - Load the real DOCX file
            var testDataPath = Path.Combine(
                Path.GetDirectoryName(typeof(RealDocumentNumberingTests).Assembly.Location)!, 
                "..", "..", "..", "TestData", "test-document.docx"
            );
            
            testDataPath.Should().NotBeNull();
            File.Exists(testDataPath).Should().BeTrue($"Test file should exist at {testDataPath}");

            using var docxStream = File.OpenRead(testDataPath);

            // Act - Process through the complete pipeline
            // Step 1: Convert DOCX to Excel
            var (excelStream, _, _) = await _docxConverter.ConvertDocxToExcelAsync(docxStream, "test-document.docx");
            
            // Step 2: Process Excel to ChecklistItems
            var checklistItems = await _excelProcessor.ProcessExcelAsync(excelStream, "test.xlsx");
            
            // Step 3: Convert to SurveyJS JSON
            var surveyJson = _surveyJSConverter.ConvertToSurveyJS(checklistItems, "Real Document Test");

            // Assert - Parse JSON and analyze the results
            surveyJson.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(surveyJson);
            
            // Extract all titles from all pages (correct structure)
            var titles = new List<string>();
            if (jsonDocument.RootElement.TryGetProperty("pages", out var pages))
            {
                for (int pageIndex = 0; pageIndex < pages.GetArrayLength(); pageIndex++)
                {
                    var page = pages[pageIndex];
                    if (page.TryGetProperty("elements", out var pageElements))
                    {
                        for (int elementIndex = 0; elementIndex < pageElements.GetArrayLength(); elementIndex++)
                        {
                            var element = pageElements[elementIndex];
                            if (element.TryGetProperty("title", out var titleProp))
                            {
                                var title = titleProp.GetString();
                                if (!string.IsNullOrEmpty(title))
                                {
                                    titles.Add(title);
                                }
                            }
                        }
                    }
                }
            }
            
            // Verify that complex numbering patterns are preserved
            // Look for patterns like "3.1", "3.1.1", "2.5", etc. that should be preserved
            var complexNumberedItems = titles.Where(title => 
                System.Text.RegularExpressions.Regex.IsMatch(title, @"^\d+\.\d+(\.\d+)*\s")  // Matches X.Y or X.Y.Z at start followed by space
            ).ToList();

            // The test should find items with complex numbering that are preserved
            complexNumberedItems.Count.Should().BeGreaterThan(0, "Document should contain items with complex numbering like '3.1' that are preserved");

            // Verify specific patterns that we can see in the JSON output
            titles.Should().Contain(t => t.StartsWith("3.1 General"), "Should preserve '3.1 General Provide that:'");
            titles.Should().Contain(t => t.StartsWith("3.1.1 The Deed"), "Should preserve '3.1.1' numbering");
            titles.Should().Contain(t => t.StartsWith("3.2 Reuse"), "Should preserve '3.2' numbering");
            titles.Should().Contain(t => t.StartsWith("3.2.1 Subject"), "Should preserve '3.2.1' numbering");

            // Verify that simple numbering is stripped
            var simpleNumberPattern = @"^[1-9]\d*\.\s+[A-Z]"; // Pattern like "1. Something", "12. Something"
            var itemsWithSimpleNumbering = titles.Where(title => 
                System.Text.RegularExpressions.Regex.IsMatch(title, simpleNumberPattern)
            ).ToList();

            // Should be 0 items with simple numbering (it should all be stripped)
            itemsWithSimpleNumbering.Count.Should().Be(0, "Simple numbering like '1. Item' should be stripped, only complex numbering like '3.1.1' should be preserved");

            // Verify the JSON structure is valid
            titles.Count.Should().BeGreaterThan(0, "Should have processed some checklist items");
        }
    }
}
