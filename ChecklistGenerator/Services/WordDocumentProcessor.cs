using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ChecklistGenerator.Models;
using System.Text.RegularExpressions;
using NPOI.XWPF.UserModel;

namespace ChecklistGenerator.Services
{
    public class WordDocumentProcessor
    {
        public async Task<List<ChecklistItem>> ProcessWordDocumentAsync(Stream documentStream, string fileName = "")
        {
            var checklistItems = new List<ChecklistItem>();

            try
            {
                // Reset stream position
                documentStream.Position = 0;
                
                // Process the document (should always be .docx format now due to conversion)
                checklistItems = await ProcessDocxDocumentAsync(documentStream);

                // If no items were extracted, provide helpful feedback
                if (checklistItems.Count == 0)
                {
                    checklistItems.Add(new ChecklistItem
                    {
                        Id = "no_items_found",
                        Text = "No checklist items were found in this document.",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = "The document may not contain recognizable checklist patterns, or may need manual review."
                    });
                }
            }
            catch (Exception ex)
            {
                // Create an informative checklist item for any processing errors
                checklistItems.Add(new ChecklistItem
                {
                    Id = "processing_error",
                    Text = $"Unable to process this document: {ex.Message}",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false,
                    Description = "The file may be corrupted, password-protected, or contain unsupported formatting."
                });
            }

            return checklistItems;
        }

        private async Task<List<ChecklistItem>> ProcessDocxDocumentAsync(Stream documentStream)
        {
            var checklistItems = new List<ChecklistItem>();

            try
            {
                // Reset stream position
                documentStream.Position = 0;
                
                // Try NPOI first for .docx files
                try
                {
                    using var doc = new XWPFDocument(documentStream);
                    checklistItems = await ProcessXWPFDocumentAsync(doc);
                }
                catch
                {
                    // Fallback to DocumentFormat.OpenXml
                    documentStream.Position = 0;
                    using var wordDocument = WordprocessingDocument.Open(documentStream, false);
                    var body = wordDocument.MainDocumentPart?.Document?.Body;

                    if (body != null)
                    {
                        var paragraphs = body.Elements<Paragraph>().ToList();
                        var tables = body.Elements<Table>().ToList();

                        // Process paragraphs
                        await ProcessParagraphs(paragraphs, checklistItems);

                        // Process tables
                        await ProcessTables(tables, checklistItems);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error processing DOCX document: {ex.Message}", ex);
            }

            return checklistItems;
        }

        private async Task<List<ChecklistItem>> ProcessXWPFDocumentAsync(XWPFDocument doc)
        {
            var checklistItems = new List<ChecklistItem>();

            await Task.Run(() =>
            {
                // Process tables first - this is where the real questions are
                foreach (var table in doc.Tables)
                {
                    Console.WriteLine($"Processing table with {table.Rows.Count} rows");
                    
                    foreach (var row in table.Rows)
                    {
                        var cells = row.GetTableCells();
                        Console.WriteLine($"Row has {cells.Count} cells");
                        
                        // Handle 4-column format: [Question Number] [Text] [Clause Number/Box] [Empty Box]
                        if (cells.Count >= 4)
                        {
                            var questionNumberText = GetFullCellText(cells[0]);
                            var questionText = GetFullCellText(cells[1]);

                            Console.WriteLine($"Raw extracted text length: {questionText.Length}");
                            Console.WriteLine($"Raw extracted text: '{questionText}'");
                            Console.WriteLine($"Examining 4-column row: '{questionNumberText}' | Text preview: '{questionText.Substring(0, Math.Min(100, questionText.Length))}...'");

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(questionText) || 
                                questionText.Length < 10 || // Increase minimum length for real questions
                                IsHeaderRow(questionNumberText, questionText) ||
                                IsCoverPageContent(questionText) ||
                                IsCoverPageContent(questionNumberText))
                            {
                                Console.WriteLine("Skipping row (header/empty/cover page)");
                                continue;
                            }

                            // Only process if question number looks like a real question number (3.1.1, etc.)
                            if (!IsValidQuestionNumber(questionNumberText))
                            {
                                Console.WriteLine($"Skipping row - invalid question number format: '{questionNumberText}'");
                                continue;
                            }

                            // Parse the question number and any sub-questions
                            var questionItems = ParseQuestionWithSubQuestions(questionNumberText, questionText);
                            checklistItems.AddRange(questionItems);
                        }
                        // Fallback to 2-column format for backwards compatibility
                        else if (cells.Count >= 2)
                        {
                            var leftColumnText = GetFullCellText(cells[0]);
                            var rightColumnText = GetFullCellText(cells[1]);

                            Console.WriteLine($"Examining 2-column row: '{leftColumnText}' | '{rightColumnText.Substring(0, Math.Min(100, rightColumnText.Length))}...'");

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(rightColumnText) || 
                                rightColumnText.Length < 10)
                            {
                                Console.WriteLine("Skipping 2-column row (empty/too short)");
                                continue;
                            }

                            // Only process if left column is a valid question number
                            if (!string.IsNullOrWhiteSpace(leftColumnText) && IsValidQuestionNumber(leftColumnText))
                            {
                                string itemId = $"item_{CleanNumberingText(leftColumnText)}";
                                string displayText = $"{leftColumnText.Trim()} {rightColumnText}";

                                var item = new ChecklistItem
                                {
                                    Id = itemId,
                                    Text = displayText,
                                    Type = ChecklistItemType.Boolean, // Default to boolean for Yes/No questions
                                    IsRequired = IsRequiredField(rightColumnText)
                                };

                                checklistItems.Add(item);
                            }
                        }
                    }
                }

                // Only process paragraphs if no table data was found
                if (checklistItems.Count == 0)
                {
                    Console.WriteLine("No table data found, processing paragraphs as fallback");
                    var itemCounter = 1;
                    
                    for (int i = 0; i < doc.Paragraphs.Count; i++)
                    {
                        var paragraph = doc.Paragraphs[i];
                        var text = paragraph.Text;
                        if (string.IsNullOrWhiteSpace(text))
                            continue;

                        // Skip cover page content
                        if (IsCoverPageContent(text))
                            continue;

                        // Check if this line starts with lowercase and should be consolidated with previous item
                        if (ShouldConsolidateWithPrevious(text) && checklistItems.Count > 0)
                        {
                            // Consolidate with the previous item
                            var lastItem = checklistItems.Last();
                            lastItem.Text = lastItem.Text.TrimEnd() + " " + text.Trim();
                            continue;
                        }

                        var item = AnalyzeTextForChecklistItem(text, itemCounter++);
                        if (item != null)
                        {
                            checklistItems.Add(item);
                        }
                    }
                }
            });

            return checklistItems;
        }

        private async Task ProcessParagraphs(List<Paragraph> paragraphs, List<ChecklistItem> checklistItems)
        {
            await Task.Run(() =>
            {
                var itemCounter = 1;
                
                for (int i = 0; i < paragraphs.Count; i++)
                {
                    var text = GetParagraphText(paragraphs[i]);
                    
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    // Skip cover page content
                    if (IsCoverPageContent(text))
                        continue;

                    // Check if this line starts with lowercase and should be consolidated with previous item
                    if (ShouldConsolidateWithPrevious(text) && checklistItems.Count > 0)
                    {
                        // Consolidate with the previous item
                        var lastItem = checklistItems.Last();
                        lastItem.Text = lastItem.Text.TrimEnd() + " " + text.Trim();
                        continue;
                    }

                    var item = AnalyzeTextForChecklistItem(text, itemCounter++);
                    if (item != null)
                    {
                        checklistItems.Add(item);
                    }
                }
            });
        }

        private async Task ProcessTables(List<Table> tables, List<ChecklistItem> checklistItems)
        {
            await Task.Run(() =>
            {
                foreach (var table in tables)
                {
                    var rows = table.Elements<TableRow>().ToList();
                    
                    foreach (var row in rows)
                    {
                        var cells = row.Elements<TableCell>().ToList();
                        
                        // Handle 4-column format: [Question Number] [Text] [Clause Number/Box] [Empty Box]
                        if (cells.Count >= 4)
                        {
                            var questionNumberText = GetFullCellText(cells[0]);
                            var questionText = GetFullCellText(cells[1]);

                            Console.WriteLine($"OpenXml: Examining 4-column row: '{questionNumberText}' | Text preview: '{questionText.Substring(0, Math.Min(100, questionText.Length))}...'");

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(questionText) || 
                                questionText.Length < 10 ||
                                IsHeaderRow(questionNumberText, questionText) ||
                                IsCoverPageContent(questionText) ||
                                IsCoverPageContent(questionNumberText))
                                continue;

                            // Only process if question number looks valid
                            if (!IsValidQuestionNumber(questionNumberText))
                                continue;

                            // Parse the question number and any sub-questions
                            var questionItems = ParseQuestionWithSubQuestions(questionNumberText, questionText);
                            checklistItems.AddRange(questionItems);
                        }
                        // Fallback to 2-column format for backwards compatibility
                        else if (cells.Count >= 2)
                        {
                            var leftColumnText = GetFullCellText(cells[0]);
                            var rightColumnText = GetFullCellText(cells[1]);

                            Console.WriteLine($"OpenXml: Examining 2-column row: '{leftColumnText}' | Text preview: '{rightColumnText.Substring(0, Math.Min(100, rightColumnText.Length))}...'");

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(rightColumnText) || 
                                rightColumnText.Length < 10)
                                continue;

                            // Use left column as the number/identifier if it contains numbering
                            string itemId;
                            string displayText;
                            
                            if (!string.IsNullOrWhiteSpace(leftColumnText) && IsValidQuestionNumber(leftColumnText))
                            {
                                // Clean the numbering text and use it as ID, but keep original for display
                                itemId = $"item_{CleanNumberingText(leftColumnText)}";
                                displayText = $"{leftColumnText} {rightColumnText}";
                            }
                            else if (!string.IsNullOrWhiteSpace(leftColumnText) && IsNumberingText(leftColumnText))
                            {
                                // Clean the numbering text and use it as ID
                                itemId = $"item_{CleanNumberingText(leftColumnText)}";
                                displayText = $"{leftColumnText} {rightColumnText}";
                            }
                            else
                            {
                                // Extract numbering from the question text itself, or use GUID
                                string extractedNumber = ExtractNumberingFromText(rightColumnText);
                                itemId = !string.IsNullOrEmpty(extractedNumber) ? extractedNumber : Guid.NewGuid().ToString();
                                displayText = rightColumnText;
                            }

                            var item = new ChecklistItem
                            {
                                Id = itemId,
                                Text = displayText,
                                Type = ChecklistItemType.Boolean, // Default to boolean for Yes/No questions
                                IsRequired = IsRequiredField(rightColumnText)
                            };

                            checklistItems.Add(item);
                        }
                    }
                }
            });
        }

        private string GetParagraphText(Paragraph paragraph)
        {
            return paragraph.InnerText ?? string.Empty;
        }

        private string GetCellText(TableCell cell)
        {
            return cell.InnerText ?? string.Empty;
        }

        // Helper method to get full text content from NPOI table cell, including all paragraphs
        private string GetFullCellText(XWPFTableCell cell)
        {
            if (cell == null)
                return string.Empty;

            var fullText = new List<string>();
            
            // Get text from all paragraphs in the cell
            foreach (var paragraph in cell.Paragraphs)
            {
                var paragraphText = paragraph.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    fullText.Add(paragraphText);
                }
            }
            
            // Join all paragraph text with spaces
            var result = string.Join(" ", fullText).Trim();
            Console.WriteLine($"GetFullCellText extracted {result.Length} characters from cell with {cell.Paragraphs.Count} paragraphs");
            
            return result;
        }

        // Helper method to get full text content from DocumentFormat.OpenXml table cell
        private string GetFullCellText(TableCell cell)
        {
            if (cell == null)
                return string.Empty;

            var fullText = new List<string>();
            
            // Get text from all paragraphs in the cell
            foreach (var paragraph in cell.Elements<Paragraph>())
            {
                var paragraphText = paragraph.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    fullText.Add(paragraphText);
                }
            }
            
            // Join all paragraph text with spaces, or fall back to InnerText if no paragraphs found
            var result = fullText.Count > 0 ? string.Join(" ", fullText).Trim() : (cell.InnerText ?? string.Empty).Trim();
            Console.WriteLine($"GetFullCellText (OpenXml) extracted {result.Length} characters from cell");
            
            return result;
        }

        private ChecklistItem? AnalyzeTextForChecklistItem(string text, int itemNumber)
        {
            // Clean up the text
            text = text.Trim();
            
            // Skip very short text, empty lines, obvious document headers/titles, or cover page content
            if (string.IsNullOrWhiteSpace(text) || 
                text.Length < 3 ||
                text.ToLower().Equals("section") ||
                text.ToLower().Equals("guidance") ||
                text.ToLower().Equals("application form") ||
                text.ToLower().Equals("checklist") ||
                text.ToLower().StartsWith("page ") ||
                IsCoverPageContent(text) ||
                Regex.IsMatch(text, @"^\d+$")) // Skip standalone numbers
                return null;

            // Extract any existing numbering from the text
            string extractedNumber = ExtractNumberingFromText(text);
            string questionText = text;

            // If we found numbering, use it; otherwise use the sequential item number
            string itemId = !string.IsNullOrEmpty(extractedNumber) ? extractedNumber : $"item_{itemNumber}";

            // Include all other text as checklist items
            return new ChecklistItem
            {
                Id = itemId,
                Text = questionText,
                Type = DetermineItemTypeFromText(questionText),
                IsRequired = IsRequiredField(questionText)
            };
        }

        // Helper method to extract numbering from text
        private string ExtractNumberingFromText(string text)
        {
            // Look for various numbering patterns at the start of text
            var patterns = new[]
            {
                @"^(\d+)\.?\s*",           // "1. " or "1 " or "12. "
                @"^(\d+\.\d+)\.?\s*",     // "1.1. " or "2.3 "
                @"^\(([a-zA-Z])\)\s*",    // "(a) " or "(A) "
                @"^([a-zA-Z])\)\s*",      // "a) " or "A) "
                @"^\((\d+)\)\s*",         // "(1) " or "(12) "
                @"^([IVXLCDM]+)\.?\s*",   // Roman numerals "I. " or "IV "
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    return $"item_{match.Groups[1].Value}";
                }
            }

            return string.Empty;
        }

        // Helper method to determine if a row is a header row
        private bool IsHeaderRow(string questionNumber, string questionText)
        {
            var lowerQuestionText = questionText.ToLower();
            var lowerQuestionNumber = questionNumber.ToLower();
            
            // Very specific header patterns only
            var isHeader = 
                // Table headers
                (lowerQuestionText.Contains("question") && lowerQuestionText.Contains("number")) ||
                (lowerQuestionText.Contains("item") && lowerQuestionText.Contains("description")) ||
                (lowerQuestionText.Contains("clause") && lowerQuestionText.Contains("number")) ||
                lowerQuestionText.Equals("text") ||
                lowerQuestionText.Equals("description") ||
                lowerQuestionNumber.Equals("no.") ||
                lowerQuestionNumber.Equals("number") ||
                lowerQuestionNumber.Contains("question") ||
                // Document titles that are too generic
                lowerQuestionText.Equals("ucits application form") ||
                lowerQuestionText.Equals("application form") ||
                lowerQuestionText.Equals("checklist") ||
                lowerQuestionText.Equals("guidance") ||
                lowerQuestionText.Equals("section") ||
                // Empty cells
                (string.IsNullOrWhiteSpace(questionNumber) && string.IsNullOrWhiteSpace(questionText));
            
            Console.WriteLine($"IsHeaderRow check: '{questionNumber}' | '{questionText}' -> {isHeader}");
            return isHeader;
        }

        // Helper method to determine if content is part of a cover page
        private bool IsCoverPageContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lowerText = text.ToLower();
            
            // Only filter out very specific cover page indicators - be very conservative
            var exactCoverPageMatches = new[]
            {
                "cover page",
                "title page", 
                "document control",
                "confidential",
                "internal use only",
                "draft version",
                "final version"
            };

            // Check if text exactly matches cover page indicators
            foreach (var indicator in exactCoverPageMatches)
            {
                if (lowerText.Equals(indicator))
                {
                    Console.WriteLine($"Cover page content detected (exact match): '{text}'");
                    return true;
                }
            }

            // Check for document control patterns with colons
            if (lowerText.Contains("prepared by:") || 
                lowerText.Contains("reviewed by:") || 
                lowerText.Contains("approved by:") ||
                lowerText.Contains("version number:") ||
                lowerText.Contains("document version:"))
            {
                Console.WriteLine($"Cover page content detected (control pattern): '{text}'");
                return true;
            }

            // Check if it's just a date pattern with no other meaningful content
            if (Regex.IsMatch(text, @"^\s*\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4}\s*$"))
            {
                Console.WriteLine($"Cover page content detected (date only): '{text}'");
                return true;
            }
            
            // Don't filter out anything else - let the header detection handle it
            Console.WriteLine($"IsCoverPageContent check: '{text}' -> false");
            return false;
        }

        // Helper method to parse a question with potential sub-questions
        private List<ChecklistItem> ParseQuestionWithSubQuestions(string questionNumber, string questionText)
        {
            var items = new List<ChecklistItem>();
            
            // Clean the question number for use as ID, but keep original for display
            var cleanedQuestionNumber = CleanQuestionNumber(questionNumber);
            var originalQuestionNumber = questionNumber.Trim();
            questionText = CleanQuestionText(questionText);
            
            if (string.IsNullOrWhiteSpace(questionText))
                return items;

            // Debug logging to understand what we're parsing
            Console.WriteLine($"Parsing Question Number: '{cleanedQuestionNumber}' (original: '{originalQuestionNumber}') with Text: '{questionText}'");

            // Check if the text contains sub-questions like (a), (b), etc.
            var subQuestionMatches = Regex.Matches(questionText, @"\(([a-z])\)\s*([^(]*?)(?=\([a-z]\)|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if (subQuestionMatches.Count > 1)
            {
                // Process each sub-question
                foreach (Match match in subQuestionMatches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var subQuestionLetter = match.Groups[1].Value.ToLower();
                        var subQuestionText = match.Groups[2].Value.Trim();
                        
                        if (!string.IsNullOrWhiteSpace(subQuestionText))
                        {
                            var itemId = $"item_{cleanedQuestionNumber}_{subQuestionLetter}";
                            var displayText = $"{originalQuestionNumber}({subQuestionLetter}) {subQuestionText}";
                            Console.WriteLine($"Creating sub-question: {itemId} - {displayText}");
                            items.Add(new ChecklistItem
                            {
                                Id = itemId,
                                Text = displayText,
                                Type = ChecklistItemType.Boolean, // Default to boolean for Yes/No questions
                                IsRequired = IsRequiredField(subQuestionText)
                            });
                        }
                    }
                }
            }
            else
            {
                // Check for alternative sub-question patterns like "a)", "b)" etc.
                var altSubQuestionMatches = Regex.Matches(questionText, @"([a-z])\)\s*([^a-z)]*?)(?=[a-z]\)|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                if (altSubQuestionMatches.Count > 1)
                {
                    // Process each alternative sub-question
                    foreach (Match match in altSubQuestionMatches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            var subQuestionLetter = match.Groups[1].Value.ToLower();
                            var subQuestionText = match.Groups[2].Value.Trim();
                            
                            if (!string.IsNullOrWhiteSpace(subQuestionText))
                            {
                                var itemId = $"item_{cleanedQuestionNumber}_{subQuestionLetter}";
                                var displayText = $"{originalQuestionNumber}{subQuestionLetter}) {subQuestionText}";
                                Console.WriteLine($"Creating alt sub-question: {itemId} - {displayText}");
                                items.Add(new ChecklistItem
                                {
                                    Id = itemId,
                                    Text = displayText,
                                    Type = ChecklistItemType.Boolean, // Default to boolean for Yes/No questions
                                    IsRequired = IsRequiredField(subQuestionText)
                                });
                            }
                        }
                    }
                }
                else
                {
                    // No sub-questions, treat as single question
                    var itemId = $"item_{cleanedQuestionNumber}";
                    var displayText = $"{originalQuestionNumber} {questionText}";
                    Console.WriteLine($"Creating main question: {itemId} - {displayText}");
                    items.Add(new ChecklistItem
                    {
                        Id = itemId,
                        Text = displayText,
                        Type = ChecklistItemType.Boolean, // Default to boolean for Yes/No questions
                        IsRequired = IsRequiredField(questionText)
                    });
                }
            }
            
            return items;
        }

        // Helper method to clean and normalize question text
        private string CleanQuestionText(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
                return string.Empty;
            
            // Replace multiple whitespaces and line breaks with single spaces, but preserve the full text
            questionText = Regex.Replace(questionText, @"\s+", " ");
            
            // Trim whitespace from start and end, but don't remove any actual content
            questionText = questionText.Trim();
            
            Console.WriteLine($"Cleaned question text: Original length={questionText.Length}, Result='{questionText}'");
            
            return questionText;
        }

        // Helper method to clean and normalize question numbers
        private string CleanQuestionNumber(string questionNumber)
        {
            if (string.IsNullOrWhiteSpace(questionNumber))
                return "unknown";
            
            // Remove common punctuation and whitespace, replace dots with underscores for valid IDs
            var cleaned = questionNumber.Trim()
                                       .TrimEnd('.', ')', ':')
                                       .Trim('(', ')')
                                       .Replace(".", "_")
                                       .Replace(" ", "_")
                                       .ToLower();
            
            // Ensure we have a valid identifier
            if (string.IsNullOrWhiteSpace(cleaned))
                return "unknown";
                
            Console.WriteLine($"Cleaned question number: '{questionNumber}' -> '{cleaned}'");
            return cleaned;
        }

        // Helper method to validate if a string looks like a valid question number
        private bool IsValidQuestionNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            
            text = text.Trim();
            
            // Valid question number patterns (like 3.1.1, 1.2, etc.)
            var validPatterns = new[]
            {
                @"^\d+\.\d+\.\d+$",      // "3.1.1"
                @"^\d+\.\d+$",           // "3.1"
                @"^\d+$",                // "3" (single numbers)
            };
            
            foreach (var pattern in validPatterns)
            {
                if (Regex.IsMatch(text, pattern))
                {
                    Console.WriteLine($"Valid question number found: '{text}'");
                    return true;
                }
            }
            
            Console.WriteLine($"Invalid question number: '{text}'");
            return false;
        }

        // Helper method to check if text is just numbering (for left column)
        private bool IsNumberingText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Check if the text is primarily numbering
            var numberingPatterns = new[]
            {
                @"^\d+\.?$",              // "1" or "1."
                @"^\d+\.\d+\.?$",         // "1.1" or "1.1."
                @"^\([a-zA-Z]\)$",        // "(a)" or "(A)"
                @"^[a-zA-Z]\)$",          // "a)" or "A)"
                @"^\(\d+\)$",             // "(1)" or "(12)"
                @"^[IVXLCDM]+\.?$",       // Roman numerals "I" or "IV."
            };

            return numberingPatterns.Any(pattern => Regex.IsMatch(text.Trim(), pattern));
        }

        // Helper method to clean numbering text for use as identifier
        private string CleanNumberingText(string text)
        {
            // Remove common punctuation and whitespace from numbering
            return text.Trim()
                      .TrimEnd('.', ')', ':')
                      .Trim('(', ')')
                      .Trim();
        }

        // Helper method to check if text should be consolidated (starts with lowercase but has exceptions)
        private bool ShouldConsolidateWithPrevious(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length == 0)
                return false;

            // Don't consolidate if it starts with numbering patterns
            var numberingPatterns = new[]
            {
                @"^\d+\.?\s*",           // "1. " or "1 " or "12. "
                @"^\d+\.\d+\.?\s*",     // "1.1. " or "2.3 "
                @"^\([a-zA-Z]\)\s*",    // "(a) " or "(A) "
                @"^[a-zA-Z]\)\s*",      // "a) " or "A) "
                @"^\(\d+\)\s*",         // "(1) " or "(12) "
                @"^[IVXLCDM]+\.?\s*",   // Roman numerals "I. " or "IV "
            };

            foreach (var pattern in numberingPatterns)
            {
                if (Regex.IsMatch(text, pattern))
                    return false;
            }

            // Don't consolidate if it contains colons (likely section headers)
            if (text.Contains(":"))
                return false;

            // Consolidate if starts with lowercase letter and no numbering
            return char.IsLower(text[0]);
        }

        private ChecklistItemType DetermineItemType(string answerText)
        {
            var lowerAnswer = answerText.ToLower();

            if (lowerAnswer.Contains("yes/no") || lowerAnswer.Contains("yes / no"))
                return ChecklistItemType.Boolean;

            if (lowerAnswer.Contains("☐") || lowerAnswer.Contains("checkbox"))
                return ChecklistItemType.Checkbox;

            if (lowerAnswer.Contains("select") || lowerAnswer.Contains("choose"))
                return ChecklistItemType.Dropdown;

            if (Regex.IsMatch(answerText, @"[a-zA-Z]\)|\d\)"))
                return ChecklistItemType.RadioGroup;

            return ChecklistItemType.Text;
        }

        private ChecklistItemType DetermineItemTypeFromText(string text)
        {
            var lowerText = text.ToLower();

            if (lowerText.Contains("yes or no") || lowerText.Contains("confirm"))
                return ChecklistItemType.Boolean;

            if (lowerText.Contains("select") || lowerText.Contains("choose from"))
                return ChecklistItemType.Dropdown;

            if (lowerText.Contains("check all") || lowerText.Contains("multiple"))
                return ChecklistItemType.Checkbox;

            return ChecklistItemType.Text;
        }

        private bool IsRequiredField(string text)
        {
            var lowerText = text.ToLower();
            return lowerText.Contains("required") || 
                   lowerText.Contains("mandatory") || 
                   lowerText.Contains("must") ||
                   text.Contains("*");
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
                // Look for simple comma-separated options
                var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    options.AddRange(parts.Select(p => p.Trim()));
                }
                else
                {
                    // Default options for yes/no questions
                    if (text.ToLower().Contains("yes") && text.ToLower().Contains("no"))
                    {
                        options.AddRange(new[] { "Yes", "No" });
                    }
                }
            }

            return options;
        }
    }
}
