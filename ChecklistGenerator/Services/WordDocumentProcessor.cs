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
                foreach (var paragraph in doc.Paragraphs)
                {
                    var text = paragraph.Text;
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

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
                            var questionText = row.GetTableCells()[0].GetText();
                            var answerText = row.GetTableCells()[1].GetText();

                            if (!string.IsNullOrWhiteSpace(questionText))
                            {
                                var item = new ChecklistItem
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Text = questionText.Trim(),
                                    Type = DetermineItemType(answerText),
                                    IsRequired = IsRequiredField(questionText)
                                };

                                if (item.Type == ChecklistItemType.RadioGroup || 
                                    item.Type == ChecklistItemType.Dropdown ||
                                    item.Type == ChecklistItemType.Checkbox)
                                {
                                    item.Options = ExtractOptions(answerText);
                                }

                                checklistItems.Add(item);
                            }
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

                foreach (var paragraph in paragraphs)
                {
                    var text = GetParagraphText(paragraph);
                    
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

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
                            var questionText = GetCellText(cells[0]);
                            var answerText = GetCellText(cells[1]);

                            if (!string.IsNullOrWhiteSpace(questionText))
                            {
                                var item = new ChecklistItem
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Text = questionText.Trim(),
                                    Type = DetermineItemType(answerText),
                                    IsRequired = IsRequiredField(questionText)
                                };

                                if (item.Type == ChecklistItemType.RadioGroup || 
                                    item.Type == ChecklistItemType.Dropdown ||
                                    item.Type == ChecklistItemType.Checkbox)
                                {
                                    item.Options = ExtractOptions(answerText);
                                }

                                checklistItems.Add(item);
                            }
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
            // Skip headers and non-question text
            if (text.Length < 10 || 
                text.ToLower().Contains("section") ||
                text.ToLower().Contains("guidance") ||
                text.ToLower().Contains("application form"))
                return null;

            // Look for question patterns
            if (text.Contains("?") || 
                text.ToLower().StartsWith("please") ||
                text.ToLower().Contains("provide") ||
                text.ToLower().Contains("confirm") ||
                text.ToLower().Contains("specify"))
            {
                return new ChecklistItem
                {
                    Id = $"item_{itemNumber}",
                    Text = text.Trim(),
                    Type = DetermineItemTypeFromText(text),
                    IsRequired = IsRequiredField(text)
                };
            }

            return null;
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
