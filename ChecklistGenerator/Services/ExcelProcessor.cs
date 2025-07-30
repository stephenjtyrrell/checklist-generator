using ChecklistGenerator.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text;

namespace ChecklistGenerator.Services
{
    public class ExcelProcessor
    {
        private readonly ILogger<ExcelProcessor> _logger;
        private readonly OpenRouterService _openRouterService;

        public ExcelProcessor(ILogger<ExcelProcessor> logger, OpenRouterService openRouterService)
        {
            _logger = logger;
            _openRouterService = openRouterService;
        }

        public async Task<List<ChecklistItem>> ProcessExcelAsync(Stream excelStream, string fileName = "")
        {
            try
            {
                excelStream.Position = 0;
                
                using var workbook = new XSSFWorkbook(excelStream);
                var sheet = workbook.GetSheetAt(0); // Process first sheet
                
                if (sheet == null)
                {
                    throw new InvalidOperationException("No worksheet found in Excel file");
                }

                // Extract all text content from the Excel sheet
                var documentContent = ExtractTextFromSheet(sheet);
                
                if (string.IsNullOrWhiteSpace(documentContent))
                {
                    throw new InvalidOperationException("No readable content found in Excel file");
                }

                _logger.LogInformation($"Extracted {documentContent.Length} characters from Excel file, processing with OpenRouter AI");

                // Use OpenRouter AI to convert the Excel content to checklist items
                var checklistItems = await _openRouterService.ConvertDocumentToChecklistAsync(documentContent, fileName);
                
                // Fallback if no items generated
                if (checklistItems.Count == 0)
                {
                    checklistItems = new List<ChecklistItem>
                    {
                        new ChecklistItem
                        {
                            Id = "no_items_found",
                            Text = "No checklist items found in document",
                            Type = ChecklistItemType.Comment,
                            IsRequired = false,
                            Description = "The AI could not identify actionable items in this document"
                        }
                    };
                }

                _logger.LogInformation($"Generated {checklistItems.Count} checklist items from Excel using AI");
                return checklistItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                
                // Create error item
                return new List<ChecklistItem>
                {
                    new ChecklistItem
                    {
                        Id = "excel_processing_error",
                        Text = "Failed to process Excel content",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = $"Error: {ex.Message}"
                    }
                };
            }
        }

        private string ExtractTextFromSheet(ISheet sheet)
        {
            var contentBuilder = new StringBuilder();
            var rowCount = sheet.LastRowNum + 1;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                var rowText = new StringBuilder();
                var cellCount = row.LastCellNum;

                for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
                {
                    var cell = row.GetCell(cellIndex);
                    if (cell != null)
                    {
                        var cellValue = GetCellValueAsString(cell);
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            if (rowText.Length > 0)
                                rowText.Append(" | ");
                            rowText.Append(cellValue.Trim());
                        }
                    }
                }

                if (rowText.Length > 0)
                {
                    contentBuilder.AppendLine(rowText.ToString());
                }
            }

            return contentBuilder.ToString();
        }

        private string GetCellValueAsString(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell) 
                    ? cell.DateCellValue?.ToString() ?? cell.NumericCellValue.ToString()
                    : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue,
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => string.Empty
                },
                _ => string.Empty
            };
        }
    }
}
