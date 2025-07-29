using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ClosedXML.Excel;
using System.Text;

namespace ChecklistGenerator.Services
{
    public class DocxToExcelConverter
    {
        private readonly ILogger<DocxToExcelConverter> _logger;
        private readonly GeminiService _geminiService;

        public DocxToExcelConverter(ILogger<DocxToExcelConverter> logger, GeminiService geminiService)
        {
            _logger = logger;
            _geminiService = geminiService;
        }

        public async Task<(Stream ExcelStream, byte[] ExcelBytes, string FileName)> ConvertDocxToExcelAsync(Stream docxStream, string originalFileName)
        {
            try
            {
                // Generate filename for download
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
                var safeFileName = string.Join("_", baseFileName.Split(Path.GetInvalidFileNameChars()));
                var downloadFileName = $"{safeFileName}_{timestamp}.xlsx";

                var tempDocxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
                var tempExcelPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                try
                {
                    // Write DOCX stream to temporary file
                    using (var fileStream = File.Create(tempDocxPath))
                    {
                        docxStream.Position = 0;
                        await docxStream.CopyToAsync(fileStream);
                    }

                    // Run the AI-powered conversion to temporary Excel file
                    await ConvertDocxToExcelWithAI(tempDocxPath, tempExcelPath, originalFileName);

                    // Read the Excel file into memory
                    var excelBytes = await File.ReadAllBytesAsync(tempExcelPath);
                    var excelStream = new MemoryStream(excelBytes);
                    
                    _logger.LogInformation($"AI-powered Excel conversion completed. Download filename: {downloadFileName}");
                    
                    return (excelStream, excelBytes, downloadFileName);
                }
                finally
                {
                    // Clean up temporary files
                    if (File.Exists(tempDocxPath))
                        File.Delete(tempDocxPath);
                    if (File.Exists(tempExcelPath))
                        File.Delete(tempExcelPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting DOCX to Excel");
                throw new InvalidOperationException($"Failed to convert DOCX to Excel: {ex.Message}", ex);
            }
        }

        private async Task ConvertDocxToExcelWithAI(string inputPath, string outputPath, string originalFileName)
        {
            try
            {
                // Extract text content from DOCX
                var documentContent = ExtractTextFromDocx(inputPath);
                
                if (string.IsNullOrWhiteSpace(documentContent))
                {
                    throw new InvalidOperationException("No readable content found in DOCX file");
                }

                _logger.LogInformation($"Extracted {documentContent.Length} characters from DOCX, processing with Gemini AI");

                // Use Gemini AI to convert document to checklist items
                var checklistItems = await _geminiService.ConvertDocumentToChecklistAsync(documentContent, originalFileName);

                // Create Excel file from AI-generated checklist items
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Checklist");
                    
                    // Set up headers
                    ws.Cell(1, 1).Value = "ID";
                    ws.Cell(1, 2).Value = "Item";
                    ws.Cell(1, 3).Value = "Description";
                    ws.Cell(1, 4).Value = "Type";
                    ws.Cell(1, 5).Value = "Required";
                    ws.Cell(1, 6).Value = "Status";

                    // Style headers
                    var headerRange = ws.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Write checklist items
                    for (int i = 0; i < checklistItems.Count; i++)
                    {
                        var item = checklistItems[i];
                        var rowIndex = i + 2;

                        ws.Cell(rowIndex, 1).Value = item.Id ?? $"item_{i + 1}";
                        ws.Cell(rowIndex, 2).Value = item.Text ?? "";
                        ws.Cell(rowIndex, 3).Value = item.Description ?? "";
                        ws.Cell(rowIndex, 4).Value = item.Type.ToString();
                        ws.Cell(rowIndex, 5).Value = item.IsRequired ? "Yes" : "No";
                        ws.Cell(rowIndex, 6).Value = ""; // Empty status column for user to fill
                    }

                    // Auto-fit columns
                    ws.ColumnsUsed().AdjustToContents();

                    // Add some basic formatting
                    ws.Column(2).Width = 50; // Make Item column wider
                    ws.Column(3).Width = 30; // Make Description column wider
                    
                    wb.SaveAs(outputPath);
                }

                _logger.LogInformation($"Successfully created Excel file with {checklistItems.Count} AI-generated checklist items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI-powered DOCX to Excel conversion");
                
                // Fallback: create basic Excel with error message
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Error");
                    ws.Cell(1, 1).Value = "Error";
                    ws.Cell(1, 2).Value = "Message";
                    ws.Cell(2, 1).Value = "Conversion Failed";
                    ws.Cell(2, 2).Value = ex.Message;
                    wb.SaveAs(outputPath);
                }
                
                throw;
            }
        }

        private string ExtractTextFromDocx(string inputPath)
        {
            var contentBuilder = new StringBuilder();

            using (var doc = WordprocessingDocument.Open(inputPath, false))
            {
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    // Extract from tables first
                    foreach (var table in body.Elements<Table>())
                    {
                        contentBuilder.AppendLine("=== TABLE ===");
                        foreach (var row in table.Elements<TableRow>())
                        {
                            var cellTexts = row.Elements<TableCell>()
                                .Select(cell => string.Concat(cell.Descendants<Text>().Select(t => t.Text)).Trim())
                                .Where(text => !string.IsNullOrWhiteSpace(text));
                            
                            if (cellTexts.Any())
                            {
                                contentBuilder.AppendLine(string.Join(" | ", cellTexts));
                            }
                        }
                        contentBuilder.AppendLine();
                    }

                    // Extract from paragraphs
                    foreach (var paragraph in body.Elements<Paragraph>())
                    {
                        var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text)).Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            contentBuilder.AppendLine(text);
                        }
                    }

                    // Extract from lists
                    foreach (var numbering in body.Descendants<NumberingProperties>())
                    {
                        var listItems = numbering.Ancestors<Paragraph>()
                            .Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)).Trim())
                            .Where(text => !string.IsNullOrWhiteSpace(text));
                        
                        foreach (var item in listItems)
                        {
                            contentBuilder.AppendLine($"• {item}");
                        }
                    }
                }
            }

            return contentBuilder.ToString();
        }
    }
}
