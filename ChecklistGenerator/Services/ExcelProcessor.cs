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
                if (IsObviousNonQuestion(content))
                {
                    return null;
                }

                // Skip standalone numbers or number patterns
                var trimmedContent = content.Trim();
                if (IsStandaloneNumbering(trimmedContent))
                {
                    return null;
                }

                // Clean the content for better analysis
                var cleanedContent = CleanQuestionText(content);

                var item = new ChecklistItem
                {
                    Id = $"item_{itemNumber:D3}",
                    Text = cleanedContent,
                    Type = DetermineQuestionType(content),
                    IsRequired = ContainsRequiredIndicator(content),
                    Description = "" // Keep description empty for cleaner UI
                };

                // Extract options if it's a multiple choice question
                if (item.Type == ChecklistItemType.RadioGroup || 
                    item.Type == ChecklistItemType.Checkbox || 
                    item.Type == ChecklistItemType.Dropdown)
                {
                    item.Options = ExtractOptions(content);
                    
                    // If no options were extracted for radio/checkbox/dropdown, 
                    // convert to boolean question
                    if (item.Options.Count == 0)
                    {
                        item.Type = ChecklistItemType.Boolean;
                        item.Options = new List<string> { "Yes", "No" };
                    }
                }
                else if (item.Type == ChecklistItemType.Boolean)
                {
                    // Ensure boolean questions have Yes/No options
                    item.Options = new List<string> { "Yes", "No" };
                }

                return item;
            });
        }

        private string CleanQuestionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove excessive whitespace
            var cleaned = Regex.Replace(text, @"\s+", " ");
            cleaned = cleaned.Trim();
            
            // Remove standalone numbering from the beginning
            cleaned = Regex.Replace(cleaned, @"^[\d\w]+[\.\)]\s*", "");
            
            // Remove bullet points
            cleaned = Regex.Replace(cleaned, @"^[•\-\*○●]\s*", "");
            
            // Capitalize first letter if it's not already
            if (cleaned.Length > 0 && char.IsLower(cleaned[0]))
            {
                cleaned = char.ToUpper(cleaned[0]) + (cleaned.Length > 1 ? cleaned.Substring(1) : "");
            }
            
            return cleaned;
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

            var lowerText = text.ToLower().Trim();
            
            // Exclude obvious non-questions
            if (IsStandaloneNumbering(text) || 
                lowerText.StartsWith("page ") || 
                lowerText.StartsWith("table ") ||
                lowerText.StartsWith("figure ") ||
                lowerText.StartsWith("section ") ||
                lowerText.StartsWith("chapter ") ||
                Regex.IsMatch(lowerText, @"^(column_|row_|cell_)"))
            {
                return false;
            }

            // Strong question indicators
            if (text.EndsWith("?") ||
                lowerText.Contains("yes/no") ||
                lowerText.Contains("true/false") ||
                Regex.IsMatch(lowerText, @"\b(what|how|when|where|why|which|who)\b") ||
                Regex.IsMatch(lowerText, @"\b(do|are|have|will|can|should|would|could|is|was|does|has)\s+(you|they|we|it)\b") ||
                lowerText.Contains("please ") ||
                lowerText.Contains("select ") ||
                lowerText.Contains("choose ") ||
                lowerText.Contains("enter ") ||
                lowerText.Contains("provide ") ||
                lowerText.Contains("specify ") ||
                lowerText.Contains("indicate ") ||
                lowerText.Contains("describe ") ||
                lowerText.Contains("explain ") ||
                lowerText.Contains("list ") ||
                lowerText.Contains("name ") ||
                lowerText.Contains("identify "))
            {
                return true;
            }

            // Check for instruction-like patterns
            if (Regex.IsMatch(lowerText, @"\b(check|mark|tick|select|choose|circle|highlight)\s+(all|any|one|the)\b"))
            {
                return true;
            }

            // Check for field-like patterns
            if (Regex.IsMatch(lowerText, @"\b(name|address|email|phone|date|number|amount|quantity|title|position|role)\b") &&
                (text.Contains(":") || text.Contains("_") || lowerText.Contains("enter") || lowerText.Contains("provide")))
            {
                return true;
            }

            // Check for option-based questions
            if (HasMultipleOptions(text))
            {
                return true;
            }

            // If it contains imperative verbs and is reasonably long, likely a question/instruction
            if (text.Length > 10 && 
                Regex.IsMatch(lowerText, @"\b(complete|fill|answer|respond|rate|evaluate|assess|review)\b"))
            {
                return true;
            }

            return false;
        }

        private bool HasMultipleOptions(string text)
        {
            // Check for numbered options
            var numberedOptions = Regex.Matches(text, @"\b\d+[\)\.]").Count;
            if (numberedOptions > 1) return true;

            // Check for lettered options
            var letteredOptions = Regex.Matches(text, @"\b[a-zA-Z][\)\.]").Count;
            if (letteredOptions > 1) return true;

            // Check for bullet points
            var bullets = text.Count(c => "•-*○●".Contains(c));
            if (bullets > 1) return true;

            // Check for "or" patterns
            if (Regex.Matches(text, @"\bor\b", RegexOptions.IgnoreCase).Count > 0 &&
                !text.ToLower().Contains("yes or no"))
            {
                return true;
            }

            return false;
        }

        private ChecklistItemType DetermineQuestionType(string text)
        {
            var lowerText = text.ToLower();
            
            // Check for explicit type indicators first
            if (lowerText.Contains("comment") || lowerText.Contains("notes") || lowerText.Contains("remarks"))
            {
                return ChecklistItemType.Comment;
            }

            // For legal/regulatory text, be very conservative about multiple choice
            // Look for explicit indicators of choice questions
            if (lowerText.Contains("select all") || lowerText.Contains("check all") || 
                lowerText.Contains("mark all") || lowerText.Contains("tick all") ||
                Regex.IsMatch(lowerText, @"\b(choose|select)\s+(all|multiple|any)\b"))
            {
                return ChecklistItemType.Checkbox;
            }

            // Check for dropdown indicators
            if (lowerText.Contains("dropdown") || lowerText.Contains("select from list") || 
                lowerText.Contains("choose from") || lowerText.Contains("pick from") ||
                Regex.IsMatch(lowerText, @"\b(select|choose)\s+from\s+(the\s+)?(list|menu|options)\b"))
            {
                return ChecklistItemType.Dropdown;
            }

            // Be more strict about radio groups - only if there are clear numbered/lettered options
            var hasExplicitOptions = Regex.IsMatch(text, @"\s+[a-zA-Z]\)\s+\w+") && Regex.Matches(text, @"\s+[a-zA-Z]\)").Count > 1;
            var hasNumberedOptions = Regex.IsMatch(text, @"\s+\d+\)\s+\w+") && Regex.Matches(text, @"\s+\d+\)").Count > 1;
            var hasBulletOptions = Regex.IsMatch(text, @"[\r\n•]\s*\w+") && Regex.Matches(text, @"[\r\n•]").Count > 1;
            
            if (hasExplicitOptions || hasNumberedOptions || hasBulletOptions)
            {
                // If it's explicitly asking for multiple selections, make it checkbox
                if (lowerText.Contains("select all") || lowerText.Contains("check all"))
                {
                    return ChecklistItemType.Checkbox;
                }
                return ChecklistItemType.RadioGroup;
            }

            // Boolean questions (Yes/No) - be more specific
            if (lowerText.Contains("yes/no") || lowerText.Contains("yes or no") ||
                lowerText.Contains("true/false") || lowerText.Contains("true or false") ||
                Regex.IsMatch(lowerText, @"\b(do|are|have|will|can|should|would|could|is|was|does|has)\s+you\b") ||
                lowerText.Contains("comply") || lowerText.Contains("ensure") ||
                (text.EndsWith("?") && text.Length < 200 && // Short questions ending with ?
                 (lowerText.StartsWith("do ") || lowerText.StartsWith("are ") || 
                  lowerText.StartsWith("have ") || lowerText.StartsWith("will ") ||
                  lowerText.StartsWith("can ") || lowerText.StartsWith("is ") ||
                  lowerText.StartsWith("does ") || lowerText.StartsWith("has "))))
            {
                return ChecklistItemType.Boolean;
            }

            // Text input questions (more comprehensive detection)
            if (lowerText.Contains("name") || lowerText.Contains("address") || 
                lowerText.Contains("description") || lowerText.Contains("details") || 
                lowerText.Contains("explain") || lowerText.Contains("describe") ||
                lowerText.Contains("list") || lowerText.Contains("specify") ||
                lowerText.Contains("provide") || lowerText.Contains("enter") ||
                lowerText.StartsWith("what") || lowerText.StartsWith("where") ||
                lowerText.StartsWith("when") || lowerText.StartsWith("how") ||
                lowerText.StartsWith("why") || lowerText.StartsWith("which") ||
                lowerText.Contains("number") || lowerText.Contains("amount") ||
                lowerText.Contains("quantity") || lowerText.Contains("date") ||
                lowerText.Contains("time") || lowerText.Contains("email") ||
                lowerText.Contains("phone") || lowerText.Contains("website"))
            {
                return ChecklistItemType.Text;
            }

            // For complex legal/regulatory text, default to comment field for long text
            if (text.Length > 300 || 
                lowerText.Contains("depositary") || lowerText.Contains("directive") ||
                lowerText.Contains("accordance") || lowerText.Contains("regulation") ||
                lowerText.Contains("shall") || lowerText.Contains("whereby"))
            {
                return ChecklistItemType.Comment;
            }

            // Default to boolean for everything else
            return ChecklistItemType.Boolean;
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

            // Strategy 1: Look for numbered options (1), 2), 3) or 1. 2. 3.)
            var numberedMatches = Regex.Matches(text, @"(?:^|\s)(\d+)[\)\.](\s*)([^\r\n\d\)\.]*?)(?=\s*\d+[\)\.]|\s*$)", RegexOptions.Multiline);
            if (numberedMatches.Count > 1)
            {
                foreach (Match match in numberedMatches)
                {
                    if (match.Groups.Count > 3)
                    {
                        var option = match.Groups[3].Value.Trim();
                        if (!string.IsNullOrEmpty(option) && option.Length > 1)
                        {
                            options.Add(CleanOptionText(option));
                        }
                    }
                }
            }

            // Strategy 2: Look for lettered options (a), b), c) or a. b. c.)
            if (options.Count == 0)
            {
                var letteredMatches = Regex.Matches(text, @"(?:^|\s)([a-zA-Z])[\)\.](\s*)([^\r\n\)\.]*?)(?=\s*[a-zA-Z][\)\.]|\s*$)", RegexOptions.Multiline);
                if (letteredMatches.Count > 1)
                {
                    foreach (Match match in letteredMatches)
                    {
                        if (match.Groups.Count > 3)
                        {
                            var option = match.Groups[3].Value.Trim();
                            if (!string.IsNullOrEmpty(option) && option.Length > 1)
                            {
                                options.Add(CleanOptionText(option));
                            }
                        }
                    }
                }
            }

            // Strategy 3: Look for bullet points (•, -, *, ○, ●)
            if (options.Count == 0)
            {
                var bulletMatches = Regex.Matches(text, @"[•\-\*○●]\s*([^\r\n•\-\*○●]+)", RegexOptions.Multiline);
                if (bulletMatches.Count > 1)
                {
                    foreach (Match match in bulletMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            var option = match.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(option) && option.Length > 1)
                            {
                                options.Add(CleanOptionText(option));
                            }
                        }
                    }
                }
            }

            // Strategy 4: Look for "or" separated options
            if (options.Count == 0)
            {
                var orMatches = Regex.Matches(text, @"\b(\w+(?:\s+\w+)*)\s+or\s+(\w+(?:\s+\w+)*)\b", RegexOptions.IgnoreCase);
                if (orMatches.Count > 0)
                {
                    foreach (Match match in orMatches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            var option1 = match.Groups[1].Value.Trim();
                            var option2 = match.Groups[2].Value.Trim();
                            
                            if (!string.IsNullOrEmpty(option1) && option1.Length > 1 && !options.Contains(option1))
                                options.Add(CleanOptionText(option1));
                            if (!string.IsNullOrEmpty(option2) && option2.Length > 1 && !options.Contains(option2))
                                options.Add(CleanOptionText(option2));
                        }
                    }
                }
            }

            // Strategy 5: Look for comma-separated options (be much more selective)
            if (options.Count == 0)
            {
                var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts.Length <= 5) // Stricter range for real options
                {
                    // Check if these look like actual options (not just random comma-separated text)
                    var validOptions = parts
                        .Select(p => CleanOptionText(p.Trim()))
                        .Where(p => !string.IsNullOrEmpty(p) && 
                                   p.Length > 2 && p.Length < 30 && // Stricter length for real options
                                   !p.ToLower().Contains("question") &&
                                   !p.ToLower().Contains("answer") &&
                                   !p.ToLower().Contains("shall") &&
                                   !p.ToLower().Contains("that") &&
                                   !p.ToLower().Contains("which") &&
                                   !p.ToLower().Contains("where") &&
                                   !p.ToLower().Contains("accordance") &&
                                   !p.Contains("(") && !p.Contains(")") && // Avoid legal parenthetical references
                                   !Regex.IsMatch(p, @"^\d+$") && // Not just numbers
                                   !Regex.IsMatch(p, @"\b(the|and|or|of|in|to|for|with|by)\b", RegexOptions.IgnoreCase)) // Avoid common articles/prepositions
                        .ToList();

                    // Only accept as options if they're all similar length and format
                    if (validOptions.Count >= 2 && validOptions.Count == parts.Length &&
                        validOptions.All(o => o.Length > 3 && o.Length < 25) &&
                        !text.ToLower().Contains("depositary") && // Avoid financial/legal jargon
                        !text.ToLower().Contains("accordance") &&
                        !text.ToLower().Contains("directive"))
                    {
                        options.AddRange(validOptions);
                    }
                }
            }

            // Strategy 6: Default boolean options for yes/no questions
            if (options.Count == 0)
            {
                var lowerText = text.ToLower();
                if (lowerText.Contains("yes/no") || lowerText.Contains("yes or no") ||
                    lowerText.Contains("true/false") || lowerText.Contains("true or false"))
                {
                    options.AddRange(new[] { "Yes", "No" });
                }
                else if (lowerText.Contains("true/false"))
                {
                    options.AddRange(new[] { "True", "False" });
                }
            }

            // Remove duplicates and return
            return options.Distinct().Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        }

        private string CleanOptionText(string option)
        {
            if (string.IsNullOrWhiteSpace(option))
                return string.Empty;

            // Remove common prefixes and suffixes
            option = option.Trim();
            option = Regex.Replace(option, @"^[\d\w][\)\.]?\s*", ""); // Remove numbering/lettering
            option = Regex.Replace(option, @"[•\-\*○●]\s*", ""); // Remove bullets
            option = option.Trim();

            // Capitalize first letter
            if (option.Length > 0)
            {
                option = char.ToUpper(option[0]) + (option.Length > 1 ? option.Substring(1) : "");
            }

            return option;
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

            // First pass: identify and combine numbered/lettered items with their text
            var processedCells = new bool[rowData.Count];
            
            for (int colIndex = 0; colIndex < rowData.Count; colIndex++)
            {
                if (processedCells[colIndex])
                    continue;

                var cellContent = rowData[colIndex];
                if (string.IsNullOrWhiteSpace(cellContent))
                    continue;

                var trimmedContent = cellContent.Trim();

                // Check if this cell contains standalone numbering
                if (IsStandaloneNumbering(trimmedContent))
                {
                    // Look for the corresponding question text in adjacent cells
                    string combinedText = trimmedContent;
                    bool foundQuestionText = false;

                    // Check the next few cells for question text
                    for (int nextColIndex = colIndex + 1; nextColIndex < Math.Min(rowData.Count, colIndex + 3); nextColIndex++)
                    {
                        if (processedCells[nextColIndex])
                            continue;

                        var nextCellContent = rowData[nextColIndex];
                        if (!string.IsNullOrWhiteSpace(nextCellContent))
                        {
                            var nextTrimmed = nextCellContent.Trim();
                            
                            // If the next cell looks like question text, combine them
                            if (LooksLikeQuestion(nextTrimmed) || 
                                (nextTrimmed.Length > 10 && !IsStandaloneNumbering(nextTrimmed)))
                            {
                                combinedText = $"{trimmedContent} {nextTrimmed}";
                                processedCells[nextColIndex] = true;
                                foundQuestionText = true;
                                break;
                            }
                        }
                    }

                    // Only create an item if we found question text to combine with the numbering
                    if (foundQuestionText)
                    {
                        var item = await AnalyzeContentForChecklistItem(combinedText, itemCounter++, "");
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }
                    
                    processedCells[colIndex] = true;
                }
                else if (LooksLikeQuestion(trimmedContent) || 
                         (trimmedContent.Length > 15 && HasMultipleOptions(trimmedContent)))
                {
                    // Process as a standalone question
                    var item = await AnalyzeContentForChecklistItem(trimmedContent, itemCounter++, "");
                    if (item != null)
                    {
                        items.Add(item);
                    }
                    processedCells[colIndex] = true;
                }
                else if (trimmedContent.Length > 20 && ContainsQuestionKeywords(trimmedContent))
                {
                    // Process longer text that might be a question even if not detected as such
                    var item = await AnalyzeContentForChecklistItem(trimmedContent, itemCounter++, "");
                    if (item != null)
                    {
                        items.Add(item);
                    }
                    processedCells[colIndex] = true;
                }
            }

            // Second pass: handle any remaining unprocessed cells that might be questions
            for (int colIndex = 0; colIndex < rowData.Count; colIndex++)
            {
                if (processedCells[colIndex])
                    continue;

                var cellContent = rowData[colIndex];
                if (string.IsNullOrWhiteSpace(cellContent))
                    continue;

                var trimmedContent = cellContent.Trim();
                
                // Be more lenient with remaining content
                if (trimmedContent.Length > 8 && 
                    !IsObviousNonQuestion(trimmedContent) &&
                    ContainsQuestionKeywords(trimmedContent))
                {
                    var item = await AnalyzeContentForChecklistItem(trimmedContent, itemCounter++, "");
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        private bool ContainsQuestionKeywords(string text)
        {
            var lowerText = text.ToLower();
            var questionKeywords = new[]
            {
                "what", "how", "when", "where", "why", "which", "who",
                "do you", "are you", "have you", "will you", "can you",
                "please", "select", "choose", "enter", "provide", "specify",
                "indicate", "describe", "explain", "list", "name", "identify",
                "check", "mark", "tick", "circle", "rate", "evaluate",
                "complete", "fill", "answer", "respond", "yes/no", "true/false"
            };

            return questionKeywords.Any(keyword => lowerText.Contains(keyword));
        }

        private bool IsObviousNonQuestion(string text)
        {
            var lowerText = text.ToLower();
            var nonQuestionPatterns = new[]
            {
                "page", "table", "figure", "section", "chapter", "header",
                "footer", "title", "subtitle", "reference", "note",
                "copyright", "version", "date", "author", "document"
            };

            return nonQuestionPatterns.Any(pattern => lowerText.StartsWith(pattern)) ||
                   Regex.IsMatch(text, @"^\d{1,3}$") || // Just a number
                   Regex.IsMatch(text, @"^[A-Z]{1,5}$"); // Just letters
        }
    }
}
