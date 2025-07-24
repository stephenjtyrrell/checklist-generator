using ChecklistGenerator.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.RegularExpressions;

namespace ChecklistGenerator.Services
{
    public class ExcelProcessor
    {
        private readonly ILogger<ExcelProcessor> _logger;

        public ExcelProcessor(ILogger<ExcelProcessor> logger)
        {
            _logger = logger;
        }

        public async Task<List<ChecklistItem>> ProcessExcelAsync(Stream excelStream, string fileName = "")
        {
            var checklistItems = new List<ChecklistItem>();

            try
            {
                excelStream.Position = 0;
                
                using var workbook = new XSSFWorkbook(excelStream);
                var sheet = workbook.GetSheetAt(0); // Process first sheet
                
                if (sheet == null)
                {
                    throw new InvalidOperationException("No worksheet found in Excel file");
                }

                // Assume first row contains headers
                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    throw new InvalidOperationException("No header row found in Excel file");
                }

                var headers = new List<string>();
                for (int i = 0; i < headerRow.LastCellNum; i++)
                {
                    var cell = headerRow.GetCell(i);
                    headers.Add(cell?.ToString()?.Trim() ?? $"Column_{i + 1}");
                }

                // Process data rows
                var itemCounter = 1;
                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue;

                    var rowData = new List<string>();
                    for (int cellIndex = 0; cellIndex < headers.Count; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        rowData.Add(cell?.ToString()?.Trim() ?? string.Empty);
                    }

                    // Skip empty rows
                    if (rowData.All(string.IsNullOrWhiteSpace))
                        continue;

                    // Try to intelligently combine cells that might be split numbering + question text
                    var processedItems = await ProcessRowForChecklistItems(rowData, headers, itemCounter);
                    checklistItems.AddRange(processedItems);
                    itemCounter += processedItems.Count;
                }

                _logger.LogInformation($"Processed {checklistItems.Count} checklist items from Excel");

                // If no specific checklist items found, create generic ones from the content
                if (checklistItems.Count == 0)
                {
                    checklistItems = await CreateGenericChecklistItems(sheet, headers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                
                // Create error item
                checklistItems.Add(new ChecklistItem
                {
                    Id = "excel_processing_error",
                    Text = "Failed to process Excel content",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false,
                    Description = $"Error: {ex.Message}"
                });
            }

            return checklistItems;
        }

        private async Task<ChecklistItem?> AnalyzeContentForChecklistItem(string content, int itemNumber, string columnHeader)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(content) || content.Length < 3)
                    return null;

                // Skip common non-question content
                var lowerContent = content.ToLower();
                if (lowerContent.Contains("page") || lowerContent.Contains("table") || 
                    lowerContent.Contains("figure") || lowerContent.StartsWith("column_"))
                {
                    return null;
                }

                // Skip standalone numbers or number patterns (like "3.1.1", "1)", "a)", etc.)
                var trimmedContent = content.Trim();
                if (IsStandaloneNumbering(trimmedContent))
                {
                    return null;
                }

                var item = new ChecklistItem
                {
                    Id = $"item_{itemNumber}",
                    Text = content.Trim(),
                    Type = DetermineQuestionType(content),
                    IsRequired = ContainsRequiredIndicator(content),
                    Description = "" // Remove column header information from user-facing output
                };

                // Extract options if it's a multiple choice question
                if (item.Type == ChecklistItemType.RadioGroup || item.Type == ChecklistItemType.Checkbox)
                {
                    item.Options = ExtractOptions(content);
                }

                return item;
            });
        }

        private async Task<List<ChecklistItem>> CreateGenericChecklistItems(ISheet sheet, List<string> headers)
        {
            return await Task.Run(() =>
            {
                var items = new List<ChecklistItem>();
                var itemCounter = 1;

                // Create items from headers if they look like questions
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    if (string.IsNullOrWhiteSpace(header) || header.StartsWith("Column_"))
                        continue;

                    if (LooksLikeQuestion(header))
                    {
                        items.Add(new ChecklistItem
                        {
                            Id = $"header_item_{itemCounter++}",
                            Text = header,
                            Type = DetermineQuestionType(header),
                            IsRequired = ContainsRequiredIndicator(header),
                            Description = "" // Remove column header reference from user output
                        });
                    }
                }

                // If still no items, create from content analysis
                if (items.Count == 0)
                {
                    for (int rowIndex = 1; rowIndex <= Math.Min(sheet.LastRowNum, 10); rowIndex++) // Analyze first 10 rows
                    {
                        var row = sheet.GetRow(rowIndex);
                        if (row == null) continue;

                        for (int cellIndex = 0; cellIndex < headers.Count; cellIndex++)
                        {
                            var cell = row.GetCell(cellIndex);
                            var content = cell?.ToString()?.Trim();
                            
                            if (!string.IsNullOrWhiteSpace(content) && LooksLikeQuestion(content))
                            {
                                items.Add(new ChecklistItem
                                {
                                    Id = $"content_item_{itemCounter++}",
                                    Text = content,
                                    Type = DetermineQuestionType(content),
                                    IsRequired = ContainsRequiredIndicator(content),
                                    Description = "" // Remove row/column reference from user output
                                });

                                if (items.Count >= 20) // Limit to avoid too many items
                                    break;
                            }
                        }
                        
                        if (items.Count >= 20)
                            break;
                    }
                }

                return items;
            });
        }

        private bool LooksLikeQuestion(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 5)
                return false;

            var lowerText = text.ToLower();
            return text.EndsWith("?") || 
                   lowerText.StartsWith("what") || lowerText.StartsWith("how") || 
                   lowerText.StartsWith("when") || lowerText.StartsWith("where") ||
                   lowerText.StartsWith("why") || lowerText.StartsWith("which") ||
                   lowerText.Contains("do you") || lowerText.Contains("are you") ||
                   lowerText.Contains("have you") || lowerText.Contains("will you") ||
                   lowerText.Contains("please") || lowerText.Contains("select") ||
                   lowerText.Contains("choose") || lowerText.Contains("enter") ||
                   lowerText.Contains("provide") || lowerText.Contains("specify");
        }

        private ChecklistItemType DetermineQuestionType(string text)
        {
            var lowerText = text.ToLower();
            
            // Check for multiple choice indicators
            if (lowerText.Contains("select all") || lowerText.Contains("check all") || lowerText.Contains("multiple"))
            {
                return ChecklistItemType.Checkbox;
            }

            if (lowerText.Contains("dropdown") || lowerText.Contains("select from") || lowerText.Contains("choose from"))
            {
                return ChecklistItemType.Dropdown;
            }

            // Check for options in the text
            var optionIndicators = new[] { "a)", "b)", "c)", "1)", "2)", "3)", "•", "-", "option" };
            if (optionIndicators.Any(indicator => lowerText.Contains(indicator)))
            {
                return ChecklistItemType.RadioGroup;
            }

            // Boolean questions
            if (text.EndsWith("?") || lowerText.Contains("yes/no") || 
                (lowerText.Contains("do you") || lowerText.Contains("are you") || 
                 lowerText.Contains("have you") || lowerText.Contains("will you")))
            {
                if (lowerText.Contains("name") || lowerText.Contains("describe") || 
                    lowerText.Contains("explain") || lowerText.Contains("details"))
                {
                    return ChecklistItemType.Text;
                }
                return ChecklistItemType.Boolean;
            }

            // Text input questions
            if (lowerText.Contains("name") || lowerText.Contains("address") || 
                lowerText.Contains("description") || lowerText.Contains("details") || 
                lowerText.StartsWith("what") || lowerText.StartsWith("where") ||
                lowerText.StartsWith("when") || lowerText.StartsWith("how"))
            {
                return ChecklistItemType.Text;
            }

            return ChecklistItemType.Boolean; // Default
        }

        private bool ContainsRequiredIndicator(string text)
        {
            var lowerText = text.ToLower();
            return text.Contains("*") || lowerText.Contains("required") || 
                   lowerText.Contains("mandatory") || lowerText.Contains("must");
        }

        private List<string> ExtractOptions(string text)
        {
            var options = new List<string>();

            // Look for numbered or lettered options
            var optionMatches = Regex.Matches(text, @"([a-zA-Z]\)|[0-9]\))\s*([^\r\n]+)");
            
            if (optionMatches.Count > 0)
            {
                foreach (Match match in optionMatches)
                {
                    if (match.Groups.Count > 2)
                    {
                        options.Add(match.Groups[2].Value.Trim());
                    }
                }
            }
            else
            {
                // Look for bullet points or dashes
                var bulletMatches = Regex.Matches(text, @"[•\-]\s*([^\r\n]+)");
                if (bulletMatches.Count > 0)
                {
                    foreach (Match match in bulletMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            options.Add(match.Groups[1].Value.Trim());
                        }
                    }
                }
                else
                {
                    // Look for simple comma-separated options
                    var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts.Length <= 10) // Reasonable number of options
                    {
                        options.AddRange(parts.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)));
                    }
                    else
                    {
                        // Default options for yes/no questions
                        var lowerText = text.ToLower();
                        if (lowerText.Contains("yes") || lowerText.Contains("no"))
                        {
                            options.AddRange(new[] { "Yes", "No" });
                        }
                    }
                }
            }

            return options.Distinct().ToList();
        }

        private bool IsStandaloneNumbering(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 20) // Numbering shouldn't be too long
                return false;

            // Check for common numbering patterns
            var numberingPatterns = new[]
            {
                @"^\d+$",                          // Pure numbers: "1", "2", "123"
                @"^\d+\.$",                        // Numbered with period: "1.", "2.", "10."
                @"^\d+\.\d+$",                     // Section numbering: "1.1", "3.2"
                @"^\d+\.\d+\.\d+$",               // Sub-section numbering: "1.1.1", "3.2.1"
                @"^\d+\.\d+\.\d+\.\d+$",          // Deep numbering: "1.1.1.1"
                @"^[a-zA-Z]\)$",                   // Letter with parenthesis: "a)", "b)", "A)"
                @"^[a-zA-Z]\.$",                   // Letter with period: "a.", "b.", "A."
                @"^\([a-zA-Z]\)$",                 // Letter in parentheses: "(a)", "(b)"
                @"^\(\d+\)$",                      // Number in parentheses: "(1)", "(2)"
                @"^[ivxlcdm]+\.$",                 // Roman numerals: "i.", "ii.", "iii."
                @"^[IVXLCDM]+\.$"                  // Capital Roman numerals: "I.", "II.", "III."
            };

            return numberingPatterns.Any(pattern => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
        }

        private async Task<List<ChecklistItem>> ProcessRowForChecklistItems(List<string> rowData, List<string> headers, int startingItemNumber)
        {
            var items = new List<ChecklistItem>();
            var itemCounter = startingItemNumber;

            for (int colIndex = 0; colIndex < rowData.Count; colIndex++)
            {
                var cellContent = rowData[colIndex];
                if (string.IsNullOrWhiteSpace(cellContent))
                    continue;

                var trimmedContent = cellContent.Trim();

                // Check if this cell contains standalone numbering
                if (IsStandaloneNumbering(trimmedContent))
                {
                    // Look ahead to the next non-empty cell to see if it contains question text
                    string combinedText = trimmedContent;
                    string combinedColumnHeader = headers[colIndex];

                    for (int nextColIndex = colIndex + 1; nextColIndex < rowData.Count; nextColIndex++)
                    {
                        var nextCellContent = rowData[nextColIndex];
                        if (!string.IsNullOrWhiteSpace(nextCellContent))
                        {
                            var nextTrimmed = nextCellContent.Trim();
                            
                            // If the next cell looks like question text, combine them
                            if (LooksLikeQuestion(nextTrimmed) || nextTrimmed.Length > 10)
                            {
                                combinedText = $"{trimmedContent} {nextTrimmed}";
                                combinedColumnHeader = ""; // Don't show column info to users
                                
                                // Mark the next cell as processed by clearing it
                                rowData[nextColIndex] = string.Empty;
                                break;
                            }
                            else
                            {
                                // If next cell doesn't look like question text, stop looking
                                break;
                            }
                        }
                    }

                    // Only create an item if we found question text to combine with the numbering
                    if (combinedText != trimmedContent)
                    {
                        var item = await AnalyzeContentForChecklistItem(combinedText, itemCounter++, combinedColumnHeader);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    // If no question text found, skip the standalone numbering
                }
                else
                {
                    // Process non-numbering content normally
                    var item = await AnalyzeContentForChecklistItem(trimmedContent, itemCounter++, "");
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }
    }
}
