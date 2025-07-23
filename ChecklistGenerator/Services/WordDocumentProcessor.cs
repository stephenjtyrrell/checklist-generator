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
                var itemCounter = 1;

                // Process paragraphs
                for (int i = 0; i < doc.Paragraphs.Count; i++)
                {
                    var paragraph = doc.Paragraphs[i];
                    var text = paragraph.Text;
                    if (string.IsNullOrWhiteSpace(text))
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

                // Process tables
                foreach (var table in doc.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        if (row.GetTableCells().Count >= 2)
                        {
                            var leftColumnText = row.GetTableCells()[0].GetText().Trim();
                            var rightColumnText = row.GetTableCells()[1].GetText().Trim();

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(rightColumnText) || 
                                rightColumnText.Length < 3)
                                continue;

                            // Use left column as the number/identifier if it contains numbering
                            string itemId;
                            if (!string.IsNullOrWhiteSpace(leftColumnText) && IsNumberingText(leftColumnText))
                            {
                                // Clean the numbering text and use it as ID
                                itemId = $"item_{CleanNumberingText(leftColumnText)}";
                            }
                            else
                            {
                                // Extract numbering from the question text itself, or use GUID
                                string extractedNumber = ExtractNumberingFromText(rightColumnText);
                                itemId = !string.IsNullOrEmpty(extractedNumber) ? extractedNumber : Guid.NewGuid().ToString();
                            }

                            var item = new ChecklistItem
                            {
                                Id = itemId,
                                Text = rightColumnText,
                                Type = DetermineItemType(rightColumnText),
                                IsRequired = IsRequiredField(rightColumnText)
                            };

                            if (item.Type == ChecklistItemType.RadioGroup || 
                                item.Type == ChecklistItemType.Dropdown ||
                                item.Type == ChecklistItemType.Checkbox)
                            {
                                item.Options = ExtractOptions(rightColumnText);
                            }

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
                        if (cells.Count >= 2)
                        {
                            var leftColumnText = GetCellText(cells[0]).Trim();
                            var rightColumnText = GetCellText(cells[1]).Trim();

                            // Skip header rows or empty rows
                            if (string.IsNullOrWhiteSpace(rightColumnText) || 
                                rightColumnText.Length < 3)
                                continue;

                            // Use left column as the number/identifier if it contains numbering
                            string itemId;
                            if (!string.IsNullOrWhiteSpace(leftColumnText) && IsNumberingText(leftColumnText))
                            {
                                // Clean the numbering text and use it as ID
                                itemId = $"item_{CleanNumberingText(leftColumnText)}";
                            }
                            else
                            {
                                // Extract numbering from the question text itself, or use GUID
                                string extractedNumber = ExtractNumberingFromText(rightColumnText);
                                itemId = !string.IsNullOrEmpty(extractedNumber) ? extractedNumber : Guid.NewGuid().ToString();
                            }

                            var item = new ChecklistItem
                            {
                                Id = itemId,
                                Text = rightColumnText,
                                Type = DetermineItemType(rightColumnText),
                                IsRequired = IsRequiredField(rightColumnText)
                            };

                            if (item.Type == ChecklistItemType.RadioGroup || 
                                item.Type == ChecklistItemType.Dropdown ||
                                item.Type == ChecklistItemType.Checkbox)
                            {
                                item.Options = ExtractOptions(rightColumnText);
                            }

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

        private ChecklistItem? AnalyzeTextForChecklistItem(string text, int itemNumber)
        {
            // Clean up the text
            text = text.Trim();
            
            // Skip very short text, empty lines, or obvious document headers/titles
            if (string.IsNullOrWhiteSpace(text) || 
                text.Length < 3 ||
                text.ToLower().Equals("section") ||
                text.ToLower().Equals("guidance") ||
                text.ToLower().Equals("application form") ||
                text.ToLower().Equals("checklist") ||
                text.ToLower().StartsWith("page ") ||
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
