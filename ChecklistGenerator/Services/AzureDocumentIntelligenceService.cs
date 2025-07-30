using Azure.AI.DocumentIntelligence;
using Azure;
using ChecklistGenerator.Models;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ChecklistGenerator.Services
{
    public class AzureDocumentIntelligenceService : IAzureDocumentIntelligenceService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly ILogger<AzureDocumentIntelligenceService> _logger;

        public AzureDocumentIntelligenceService(
            IConfiguration configuration, 
            ILogger<AzureDocumentIntelligenceService> logger)
        {
            _logger = logger;

            var endpoint = configuration["AzureDocumentIntelligence:Endpoint"];
            var apiKey = configuration["AzureDocumentIntelligence:ApiKey"];

            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentException("AzureDocumentIntelligence:Endpoint not configured");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("AzureDocumentIntelligence:ApiKey not configured");
            }

            // Log configuration (without sensitive data)
            _logger.LogInformation($"Initializing Azure Document Intelligence with endpoint: {endpoint.Substring(0, Math.Min(50, endpoint.Length))}...");

            try
            {
                _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
                _logger.LogInformation("Azure Document Intelligence client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Document Intelligence client with endpoint: {Endpoint}", endpoint);
                throw new InvalidOperationException($"Failed to initialize Azure Document Intelligence client: {ex.Message}", ex);
            }
        }

        public async Task<List<ChecklistItem>> ProcessDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                if (documentStream == null)
                {
                    throw new ArgumentNullException(nameof(documentStream), "Document stream cannot be null");
                }

                if (documentStream.Length == 0)
                {
                    throw new ArgumentException("Document stream is empty", nameof(documentStream));
                }

                _logger.LogInformation($"Starting Azure Document Intelligence processing for {fileName} (Size: {documentStream.Length} bytes)");

                // Reset stream position and validate content
                documentStream.Position = 0;
                
                // Read a small portion to validate it's actually a document
                var buffer = new byte[Math.Min(1024, (int)documentStream.Length)];
                var bytesRead = await documentStream.ReadAsync(buffer, 0, buffer.Length);
                documentStream.Position = 0; // Reset again
                
                _logger.LogInformation($"Document preview: First {bytesRead} bytes read. File starts with: {Convert.ToHexString(buffer.Take(16).ToArray())}");
                
                // Validate file extension/type - only PDF supported
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExtension) || !fileExtension.Equals(".pdf"))
                {
                    throw new ArgumentException($"Unsupported file type: {fileExtension}. Only PDF files are supported.");
                }

                // Check for valid PDF file signature (%PDF)
                if (fileExtension.Equals(".pdf") && bytesRead >= 4)
                {
                    var isValidPdf = buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;
                    if (!isValidPdf)
                    {
                        throw new ArgumentException($"File {fileName} does not appear to be a valid PDF file (missing PDF signature)");
                    }
                    else
                    {
                        _logger.LogInformation($"File {fileName} appears to be a valid PDF file");
                    }
                }

                try
                {
                    // Create the analyze request with better error handling
                    _logger.LogInformation($"Calling Azure Document Intelligence API for {fileName}");
                    
                    var operation = await _client.AnalyzeDocumentAsync(
                        WaitUntil.Completed,
                        "prebuilt-layout", // Use the prebuilt layout model for document structure
                        BinaryData.FromStream(documentStream));

                    var analyzeResult = operation.Value;
                    
                    _logger.LogInformation($"Document analysis completed successfully. Found {analyzeResult.Pages?.Count ?? 0} pages");

                    if (analyzeResult.Pages == null || analyzeResult.Pages.Count == 0)
                    {
                        throw new InvalidOperationException("No pages found in document analysis result");
                    }

                    // Extract structured content from the analysis result
                    var structuredContent = ExtractStructuredContent(analyzeResult);
                    
                    _logger.LogInformation($"Extracted structured content: {structuredContent.Length} characters");

                    if (string.IsNullOrWhiteSpace(structuredContent))
                    {
                        throw new InvalidOperationException("No readable content extracted from document");
                    }

                    // Generate checklist items directly from the structured content
                    var checklistItems = GenerateChecklistFromContent(structuredContent, fileName);

                    _logger.LogInformation($"Successfully created {checklistItems.Count} checklist items from document");

                    return checklistItems;
                }
                catch (Azure.RequestFailedException azureEx)
                {
                    _logger.LogError(azureEx, $"Azure Document Intelligence API error for {fileName}: Status={azureEx.Status}, Code={azureEx.ErrorCode}, Message={azureEx.Message}");
                    
                    // Provide more specific error messages based on error codes
                    var errorMessage = azureEx.ErrorCode switch
                    {
                        "InvalidRequest" => "The document format is not supported or the content is invalid. Please ensure you're uploading a valid PDF file.",
                        "UnsupportedMediaType" => "The file type is not supported. Please upload a PDF file.",
                        "FileSizeExceeded" => "The file is too large. Please upload a smaller file.",
                        "Unauthorized" => "Azure Document Intelligence API key is invalid or expired.",
                        _ => $"Azure Document Intelligence service error: {azureEx.Message}"
                    };
                    
                    _logger.LogWarning($"Azure Document Intelligence failed, attempting fallback processing: {errorMessage}");
                    
                    // Fallback: try to process document without Azure Document Intelligence
                    return ProcessDocumentFallback(documentStream, fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error during Azure Document Intelligence processing for {fileName}");
                    
                    // Fallback: try to process document without Azure Document Intelligence
                    return ProcessDocumentFallback(documentStream, fileName);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Invalid argument for document {fileName}: {ex.Message}");
                throw;
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, $"Invalid Azure Document Intelligence endpoint format: {ex.Message}");
                throw new InvalidOperationException($"Azure Document Intelligence service configuration error: Invalid endpoint format", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing document {fileName} with Azure Document Intelligence");
                throw new InvalidOperationException($"Failed to process document with Azure Document Intelligence: {ex.Message}", ex);
            }
        }

        private List<ChecklistItem> ProcessDocumentFallback(Stream documentStream, string fileName)
        {
            try
            {
                _logger.LogInformation($"Using fallback processing for {fileName}");
                
                // Reset stream position
                documentStream.Position = 0;
                
                // For now, create a basic fallback item
                var fallbackItems = new List<ChecklistItem>
                {
                    new ChecklistItem
                    {
                        Id = "azure_ai_fallback",
                        Text = "Document uploaded successfully - manual review required",
                        Description = $"Azure Document Intelligence was unable to process {fileName}. Please review the document manually for compliance requirements.",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Options = new List<string>()
                    },
                    new ChecklistItem
                    {
                        Id = "manual_review_required",
                        Text = "Confirm document has been manually reviewed",
                        Description = "Check that all requirements and compliance points in the document have been addressed.",
                        Type = ChecklistItemType.Checkbox,
                        IsRequired = true,
                        Options = new List<string>()
                    }
                };
                
                _logger.LogInformation($"Created {fallbackItems.Count} fallback checklist items");
                return fallbackItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in fallback processing for {fileName}");
                
                return new List<ChecklistItem>
                {
                    new ChecklistItem
                    {
                        Id = "processing_error",
                        Text = "Document processing failed",
                        Description = $"Unable to process {fileName}. Error: {ex.Message}",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Options = new List<string>()
                    }
                };
            }
        }

        private List<ChecklistItem> GenerateChecklistFromContent(string structuredContent, string fileName)
        {
            var checklistItems = new List<ChecklistItem>();
            
            try
            {
                var lines = structuredContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var itemCounter = 1;
                var currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                    // Track sections for context
                    if (trimmedLine.StartsWith("#"))
                    {
                        currentSection = trimmedLine.Replace("#", "").Trim();
                        continue;
                    }

                    // Skip table separators but process table content
                    if (trimmedLine == "=== TABLE ===") continue;

                    var formItem = CreateFormItemFromLine(trimmedLine, itemCounter, fileName, currentSection);
                    if (formItem != null)
                    {
                        checklistItems.Add(formItem);
                        itemCounter++;
                    }
                }

                // If no items were generated, create a basic form structure
                if (checklistItems.Count == 0)
                {
                    checklistItems.AddRange(CreateFallbackFormItems(fileName));
                }

                return checklistItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating form from content");
                
                // Return a fallback form in case of error
                return CreateFallbackFormItems(fileName, ex.Message);
            }
        }

        private ChecklistItem? CreateFormItemFromLine(string line, int counter, string fileName, string section = "")
        {
            // Skip very short lines
            if (line.Length < 3) return null;

            // Clean up the text first
            var cleanText = CleanLineText(line);
            if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 2)
                return null;

            // Skip common non-form content
            if (ShouldSkipLine(cleanText))
                return null;

            // Determine the most appropriate form field type based on content
            var formType = DetermineFormFieldType(cleanText);
            var isRequired = IsFieldRequired(cleanText);
            
            var formItem = new ChecklistItem
            {
                Id = $"field_{counter:D3}",
                Text = FormatFieldLabel(cleanText, formType),
                Description = CreateFieldDescription(cleanText, section, fileName),
                Type = formType,
                IsRequired = isRequired,
                Options = CreateFieldOptions(cleanText, formType)
            };

            return formItem;
        }

        private string CleanLineText(string line)
        {
            // Remove common prefixes and formatting
            var cleanText = line.Trim();
            cleanText = Regex.Replace(cleanText, @"^\s*[-•*]\s*", "").Trim();
            cleanText = Regex.Replace(cleanText, @"^[0-9]+\.[0-9]*\s*", "").Trim();
            cleanText = Regex.Replace(cleanText, @"^[a-zA-Z]\)\s*", "").Trim();
            cleanText = Regex.Replace(cleanText, @"^[IVX]+\.\s*", "").Trim(); // Roman numerals
            cleanText = Regex.Replace(cleanText, @"^\([0-9]+\)\s*", "").Trim(); // (1), (2), etc.
            
            return cleanText;
        }

        private bool ShouldSkipLine(string cleanText)
        {
            var skipPatterns = new[]
            {
                @"^(page|section|chapter|appendix|figure|table|exhibit)\s+[0-9]",
                @"^(copyright|©|all rights reserved)",
                @"^\d{4}-\d{2}-\d{2}", // dates
                @"^[A-Z\s]{10,}$", // long all caps lines (often headers)
                @"^_{3,}|^-{3,}|^={3,}", // separator lines
                @"^\s*$", // empty lines
                @"^(continued|cont\.?|see above|see below|n/a|not applicable)$"
            };

            return skipPatterns.Any(pattern => 
                Regex.IsMatch(cleanText, pattern, RegexOptions.IgnoreCase));
        }

        private ChecklistItemType DetermineFormFieldType(string text)
        {
            // Questions typically become comment/text fields
            if (text.TrimEnd().EndsWith("?"))
            {
                return ChecklistItemType.Comment;
            }

            // Yes/No or true/false patterns become boolean
            if (Regex.IsMatch(text, @"\b(yes/no|true/false|confirm|verified?|approved?)\b", RegexOptions.IgnoreCase))
            {
                return ChecklistItemType.Boolean;
            }

            // Multiple choice indicators become dropdown
            if (Regex.IsMatch(text, @"\b(select|choose|pick|option|type|category)\b.*\b(from|one of|between)\b", RegexOptions.IgnoreCase) ||
                text.Contains("|") || text.Contains("/") && text.Count(c => c == '/') > 1)
            {
                return ChecklistItemType.Dropdown;
            }

            // Fields that ask for specific information become text fields
            if (Regex.IsMatch(text, @"\b(name|address|phone|email|date|amount|number|describe|explain|provide|enter|specify)\b", RegexOptions.IgnoreCase))
            {
                return ChecklistItemType.Text;
            }

            // Compliance and verification items become checkboxes
            if (Regex.IsMatch(text, @"\b(must|shall|required|mandatory|ensure|verify|confirm|check|comply|attest)\b", RegexOptions.IgnoreCase))
            {
                return ChecklistItemType.Checkbox;
            }

            // Default to comment for descriptive text
            return ChecklistItemType.Comment;
        }

        private bool IsFieldRequired(string text)
        {
            return Regex.IsMatch(text, @"\b(required|mandatory|must|shall|essential|compulsory|\*)\b", RegexOptions.IgnoreCase);
        }

        private string FormatFieldLabel(string text, ChecklistItemType type)
        {
            // Remove redundant words based on field type
            switch (type)
            {
                case ChecklistItemType.Boolean:
                    // Remove yes/no from the label since it's implied
                    text = Regex.Replace(text, @"\s*\(?(yes/no|true/false)\)?", "", RegexOptions.IgnoreCase).Trim();
                    break;
                case ChecklistItemType.Text:
                    // Ensure it's phrased as a question or instruction
                    if (!text.EndsWith("?") && !text.EndsWith(":"))
                    {
                        text = text.TrimEnd('.', ',') + ":";
                    }
                    break;
                case ChecklistItemType.Checkbox:
                    // Ensure it's actionable
                    if (!Regex.IsMatch(text, @"^(check|confirm|verify|ensure|i|we)", RegexOptions.IgnoreCase))
                    {
                        text = "Confirm: " + text.ToLowerInvariant();
                    }
                    break;
            }

            return text;
        }

        private string CreateFieldDescription(string originalText, string section, string fileName)
        {
            var description = "";
            
            if (!string.IsNullOrEmpty(section))
            {
                description = $"From section: {section}";
            }
            
            if (description.Length > 0)
            {
                description += $" | Source: {fileName}";
            }
            else
            {
                description = $"Extracted from {fileName}";
            }

            return description;
        }

        private List<string> CreateFieldOptions(string text, ChecklistItemType type)
        {
            var options = new List<string>();

            switch (type)
            {
                case ChecklistItemType.Dropdown:
                    // Try to extract options from the text
                    var optionPatterns = new[]
                    {
                        @"\b(?:select|choose|pick)\s+(?:from|one of|between)[:]\s*(.+)",
                        @"\b(?:options?|choices?)[:]\s*(.+)",
                        @"(.+)\s+(?:or|/)\s+(.+)\s+(?:or|/)\s+(.+)", // A or B or C pattern
                    };

                    foreach (var pattern in optionPatterns)
                    {
                        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var optionsText = match.Groups[1].Value;
                            var extractedOptions = optionsText
                                .Split(new string[] { ",", "/", "|", " or ", " and " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.Trim().Trim('"', '\''))
                                .Where(o => !string.IsNullOrWhiteSpace(o) && o.Length > 1)
                                .Take(10) // Limit to reasonable number
                                .ToList();
                            
                            if (extractedOptions.Count > 1)
                            {
                                options.AddRange(extractedOptions);
                                break;
                            }
                        }
                    }

                    // If no options found, create some generic ones
                    if (options.Count == 0)
                    {
                        options.AddRange(new[] { "Yes", "No", "Not Applicable" });
                    }
                    break;

                case ChecklistItemType.Boolean:
                    // Boolean fields don't need explicit options
                    break;

                default:
                    // Other types don't typically need options
                    break;
            }

            return options;
        }

        private List<ChecklistItem> CreateFallbackFormItems(string fileName, string errorMessage = "")
        {
            var fallbackItems = new List<ChecklistItem>();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                fallbackItems.Add(new ChecklistItem
                {
                    Id = "processing_error",
                    Text = "Document processing encountered an issue",
                    Description = $"Error processing {fileName}: {errorMessage}",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false,
                    Options = new List<string>()
                });
            }

            // Create a basic form structure
            fallbackItems.AddRange(new[]
            {
                new ChecklistItem
                {
                    Id = "document_title",
                    Text = "Document Title:",
                    Description = $"Please enter the title of {fileName}",
                    Type = ChecklistItemType.Text,
                    IsRequired = true,
                    Options = new List<string>()
                },
                new ChecklistItem
                {
                    Id = "review_status",
                    Text = "Document Review Status:",
                    Description = "Select the current status of this document review",
                    Type = ChecklistItemType.Dropdown,
                    IsRequired = true,
                    Options = new List<string> { "Not Started", "In Progress", "Completed", "Requires Revision" }
                },
                new ChecklistItem
                {
                    Id = "reviewer_name",
                    Text = "Reviewer Name:",
                    Description = "Enter the name of the person reviewing this document",
                    Type = ChecklistItemType.Text,
                    IsRequired = true,
                    Options = new List<string>()
                },
                new ChecklistItem
                {
                    Id = "review_complete",
                    Text = "I confirm that I have thoroughly reviewed this document",
                    Description = "Check to confirm completion of document review",
                    Type = ChecklistItemType.Checkbox,
                    IsRequired = true,
                    Options = new List<string>()
                },
                new ChecklistItem
                {
                    Id = "additional_comments",
                    Text = "Additional Comments:",
                    Description = "Any additional notes or observations about this document",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false,
                    Options = new List<string>()
                }
            });

            return fallbackItems;
        }

        private string ExtractStructuredContent(AnalyzeResult result)
        {
            var contentBuilder = new StringBuilder();

            try
            {
                // Extract paragraphs with their role and content
                if (result.Paragraphs != null)
                {
                    foreach (var paragraph in result.Paragraphs)
                    {
                        var role = paragraph.Role?.ToString() ?? "content";
                        var content = paragraph.Content?.Trim();
                        
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            switch (role.ToLower())
                            {
                                case "title":
                                    contentBuilder.AppendLine($"# {content}");
                                    break;
                                case "sectionheading":
                                    contentBuilder.AppendLine($"## {content}");
                                    break;
                                case "pageheader":
                                case "pagefooter":
                                    contentBuilder.AppendLine($"*{content}*");
                                    break;
                                default:
                                    contentBuilder.AppendLine(content);
                                    break;
                            }
                            contentBuilder.AppendLine();
                        }
                    }
                }

                // Extract tables with structure
                if (result.Tables != null)
                {
                    foreach (var table in result.Tables)
                    {
                        contentBuilder.AppendLine("=== TABLE ===");
                        
                        // Group cells by row
                        var rowGroups = table.Cells
                            .GroupBy(cell => cell.RowIndex)
                            .OrderBy(group => group.Key);

                        foreach (var rowGroup in rowGroups)
                        {
                            var cellContents = rowGroup
                                .OrderBy(cell => cell.ColumnIndex)
                                .Select(cell => cell.Content?.Trim() ?? "")
                                .Where(content => !string.IsNullOrWhiteSpace(content));

                            if (cellContents.Any())
                            {
                                contentBuilder.AppendLine(string.Join(" | ", cellContents));
                            }
                        }
                        contentBuilder.AppendLine();
                    }
                }

                // If no structured content was found, extract from pages
                if (contentBuilder.Length == 0 && result.Pages != null)
                {
                    _logger.LogWarning("No structured content found, extracting from pages");
                    
                    foreach (var page in result.Pages)
                    {
                        if (page.Lines != null)
                        {
                            foreach (var line in page.Lines)
                            {
                                var content = line.Content?.Trim();
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    contentBuilder.AppendLine(content);
                                }
                            }
                        }
                    }
                }

                var extractedContent = contentBuilder.ToString();
                
                if (string.IsNullOrWhiteSpace(extractedContent))
                {
                    throw new InvalidOperationException("No readable content found in document");
                }

                return extractedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting structured content from Azure Document Intelligence result");
                throw;
            }
        }
    }
}
