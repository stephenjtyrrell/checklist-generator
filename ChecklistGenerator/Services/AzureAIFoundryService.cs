using ChecklistGenerator.Models;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.Json.Serialization;
using System.Linq;

namespace ChecklistGenerator.Services
{
    public class AzureAIFoundryService : IAzureAIFoundryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureAIFoundryService> _logger;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _deploymentName;

        public AzureAIFoundryService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<AzureAIFoundryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            _endpoint = configuration["AzureAIFoundry:Endpoint"] ?? throw new ArgumentException("AzureAIFoundry:Endpoint not configured");
            _apiKey = configuration["AzureAIFoundry:ApiKey"] ?? throw new ArgumentException("AzureAIFoundry:ApiKey not configured");
            _deploymentName = configuration["AzureAIFoundry:DeploymentName"] ?? "deepseek-r1";
            
            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            _logger.LogInformation($"Initialized Azure AI Foundry service with endpoint: {_endpoint.Substring(0, Math.Min(50, _endpoint.Length))}... and deployment: {_deploymentName}");
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

                _logger.LogInformation($"Starting Azure AI Foundry processing for {fileName} (Size: {documentStream.Length} bytes)");

                // Extract text from document
                var documentText = await ExtractTextFromDocumentAsync(documentStream, fileName);
                
                if (string.IsNullOrWhiteSpace(documentText))
                {
                    throw new InvalidOperationException("No readable content extracted from document");
                }

                // Generate checklist using Azure AI Foundry with DeepSeek R1
                var checklistItems = await ConvertDocumentToChecklistAsync(documentText, fileName);

                _logger.LogInformation($"Successfully created {checklistItems.Count} checklist items from document");

                return checklistItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing document {fileName} with Azure AI Foundry");
                throw; // Re-throw the exception instead of using fallback
            }
        }

        public async Task<string> ExtractTextFromDocumentAsync(Stream documentStream, string fileName)
        {
            try
            {
                documentStream.Position = 0;
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

                switch (fileExtension)
                {
                    case ".pdf":
                        return await ExtractTextFromPdfAsync(documentStream);
                    case ".docx":
                        return await ExtractTextFromDocxAsync(documentStream);
                    case ".txt":
                        using (var reader = new StreamReader(documentStream))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    default:
                        throw new ArgumentException($"Unsupported file type: {fileExtension}. Supported types: PDF, DOCX, TXT");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from {fileName}");
                throw;
            }
        }

        private Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
        {
            try
            {
                var textBuilder = new StringBuilder();
                
                using var pdfReader = new PdfReader(pdfStream);
                using var pdfDocument = new PdfDocument(pdfReader);
                
                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var page = pdfDocument.GetPage(pageNum);
                    var text = PdfTextExtractor.GetTextFromPage(page);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        textBuilder.AppendLine(text);
                    }
                }

                return Task.FromResult(textBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw new InvalidOperationException("Failed to extract text from PDF document", ex);
            }
        }

        private Task<string> ExtractTextFromDocxAsync(Stream docxStream)
        {
            try
            {
                var textBuilder = new StringBuilder();
                
                using var document = WordprocessingDocument.Open(docxStream, false);
                var body = document.MainDocumentPart?.Document?.Body;
                
                if (body != null)
                {
                    foreach (var paragraph in body.Elements<Paragraph>())
                    {
                        var paragraphText = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(paragraphText))
                        {
                            textBuilder.AppendLine(paragraphText);
                        }
                    }
                    
                    // Also extract text from tables
                    foreach (var table in body.Elements<Table>())
                    {
                        foreach (var row in table.Elements<TableRow>())
                        {
                            var rowText = new StringBuilder();
                            foreach (var cell in row.Elements<TableCell>())
                            {
                                var cellText = cell.InnerText?.Trim();
                                if (!string.IsNullOrWhiteSpace(cellText))
                                {
                                    if (rowText.Length > 0)
                                        rowText.Append(" | ");
                                    rowText.Append(cellText);
                                }
                            }
                            if (rowText.Length > 0)
                            {
                                textBuilder.AppendLine(rowText.ToString());
                            }
                        }
                    }
                }

                return Task.FromResult(textBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOCX");
                throw new InvalidOperationException("Failed to extract text from DOCX document", ex);
            }
        }

        public async Task<List<ChecklistItem>> ConvertDocumentToChecklistAsync(string documentContent, string fileName = "")
        {
            try
            {
                // Truncate if content is too large for the model
                if (documentContent.Length > 100000) // Adjust based on model limits
                {
                    _logger.LogWarning($"Document is large ({documentContent.Length} characters), truncating for processing");
                    documentContent = documentContent.Substring(0, 100000) + "\n\n[Content truncated for processing]";
                }

                var systemPrompt = @"You are an expert document analyst specializing in creating comprehensive compliance checklists. 
Your task is to analyze documents and extract actionable items, requirements, and compliance points.

For each checklist item, determine the most appropriate type:
- 'Checkbox' for actionable items, requirements, or verification tasks
- 'Text' for items requiring user input (names, dates, amounts, descriptions)
- 'Dropdown' for items with multiple choice options
- 'Boolean' for simple yes/no questions
- 'Comment' for informational content or explanations

Focus on quality over quantity. Extract the most important and actionable items.
Ensure each item is clear, specific, and actionable.";

                var userPrompt = $@"Analyze this document and create a structured checklist. Extract actionable items, requirements, and compliance points.

Document filename: {fileName}

Document content:
{documentContent}

CRITICAL: You MUST return ONLY a JSON array of objects with the EXACT structure shown below. Do not include any other text, explanations, or formatting.

Required JSON format - each object MUST have ALL these fields:
[
  {{
    ""id"": ""unique_identifier_1"",
    ""text"": ""Clear, actionable checklist item text"",
    ""description"": ""Additional context or explanation"",
    ""type"": ""Checkbox"",
    ""isRequired"": true,
    ""options"": []
  }},
  {{
    ""id"": ""unique_identifier_2"", 
    ""text"": ""Another checklist item"",
    ""description"": ""More details about this item"",
    ""type"": ""Text"",
    ""isRequired"": false,
    ""options"": []
  }}
]

RULES:
1. ""type"" MUST be exactly one of: ""Checkbox"", ""Text"", ""Dropdown"", ""Boolean"", ""Comment""
2. ""isRequired"" MUST be true or false (boolean, not string)
3. ""options"" MUST be an array (use [] if empty)
4. ALL fields are mandatory - do not omit any
5. ""id"" should be unique and descriptive (use underscores, no spaces)

Return ONLY the JSON array. No markdown formatting, no explanations, no additional text.";

                var requestPayload = new
                {
                    model = _deploymentName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 4000,
                    temperature = 0.3,
                    top_p = 0.8
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var requestUrl = $"{_endpoint}/models/chat/completions?api-version=2024-05-01-preview";
                _logger.LogInformation($"Sending request to Azure AI Foundry: {requestUrl}");

                var response = await _httpClient.PostAsync(requestUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Azure AI Foundry API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Azure AI Foundry API error: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"Received response from Azure AI Foundry: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");

                // Parse the response
                var responseJson = JsonDocument.Parse(responseContent);
                var aiResponse = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    throw new InvalidOperationException("Empty response from Azure AI Foundry");
                }

                // Log the full response for debugging
                _logger.LogInformation($"Raw AI response length: {aiResponse.Length} characters");
                _logger.LogDebug($"Raw AI response (first 1000 chars): {aiResponse.Substring(0, Math.Min(1000, aiResponse.Length))}");
                
                // DeepSeek R1 model outputs reasoning first, then the final answer
                // We need to extract just the JSON array from the response
                string jsonContent = ExtractJsonFromReasoningResponse(aiResponse);
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogError($"Could not extract valid JSON from AI response. Response length: {aiResponse.Length}");
                    throw new InvalidOperationException($"Could not extract JSON from AI response. Response too long to display fully.");
                }

                // Parse the checklist items
                _logger.LogDebug($"Attempting to parse JSON content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                
                List<ChecklistItem>? checklistItems;
                try
                {
                    checklistItems = JsonSerializer.Deserialize<List<ChecklistItem>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        Converters = { new ChecklistItemTypeConverter() }
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"JSON parsing failed as array. Content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                    _logger.LogError($"JSON error: {jsonEx.Message}");
                    
                    // Try to clean and repair the JSON first
                    string cleanedJson = CleanAndRepairJson(jsonContent);
                    _logger.LogDebug($"Attempting to parse cleaned JSON: {cleanedJson.Substring(0, Math.Min(500, cleanedJson.Length))}");
                    
                    try
                    {
                        checklistItems = JsonSerializer.Deserialize<List<ChecklistItem>>(cleanedJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            Converters = { new ChecklistItemTypeConverter() }
                        });
                    }
                    catch (JsonException)
                    {
                        // Check if AI returned a simple string array instead of ChecklistItem objects
                        try
                        {
                            var stringArray = JsonSerializer.Deserialize<string[]>(cleanedJson);
                            if (stringArray != null && stringArray.Length > 0)
                            {
                                _logger.LogWarning("AI returned string array instead of ChecklistItem objects. Converting...");
                                checklistItems = ConvertStringArrayToChecklistItems(stringArray);
                            }
                            else
                            {
                                throw new InvalidOperationException($"AI returned empty or invalid string array");
                            }
                        }
                        catch (JsonException)
                        {
                            // Try to parse as a single object and wrap in array
                            try
                            {
                                var singleItem = JsonSerializer.Deserialize<ChecklistItem>(cleanedJson, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    AllowTrailingCommas = true,
                                    ReadCommentHandling = JsonCommentHandling.Skip,
                                    Converters = { new ChecklistItemTypeConverter() }
                                });
                                
                                if (singleItem != null)
                                {
                                    _logger.LogInformation("Successfully parsed as single checklist item");
                                    checklistItems = new List<ChecklistItem> { singleItem };
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Failed to parse AI response as valid JSON. Extracted content: {jsonContent.Substring(0, Math.Min(300, jsonContent.Length))}", jsonEx);
                                }
                            }
                            catch (JsonException innerEx)
                            {
                                _logger.LogError($"Also failed to parse as single item: {innerEx.Message}");
                                throw new InvalidOperationException($"Failed to parse AI response as valid JSON (tried both array and single object). Extracted content: {jsonContent.Substring(0, Math.Min(300, jsonContent.Length))}", jsonEx);
                            }
                        }
                    }
                }

                if (checklistItems == null || checklistItems.Count == 0)
                {
                    throw new InvalidOperationException("Azure AI Foundry did not generate any checklist items");
                }

                // Validate and clean up the items
                ValidateAndCleanupChecklistItems(checklistItems);

                _logger.LogInformation($"Successfully generated {checklistItems.Count} checklist items");
                return checklistItems;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from Azure AI Foundry");
                throw new InvalidOperationException("Failed to parse AI response as valid JSON", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Azure AI Foundry API");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Azure AI Foundry processing");
                throw;
            }
        }

        private string ExtractJsonFromReasoningResponse(string aiResponse)
        {
            _logger.LogDebug($"Extracting JSON from reasoning response (length: {aiResponse.Length})");
            
            // For DeepSeek R1, the response contains reasoning followed by the final answer
            // We need to find the last JSON array in the response
            
            // First try to find JSON in markdown code blocks
            var markdownMatches = Regex.Matches(aiResponse, @"```(?:json)?\s*(\[.*?\])\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (markdownMatches.Count > 0)
            {
                // Return the last (most likely final) JSON block
                var lastMatch = markdownMatches[markdownMatches.Count - 1];
                _logger.LogDebug("Found JSON in markdown code block");
                return lastMatch.Groups[1].Value.Trim();
            }
            
            // Try more aggressive patterns for very long responses
            // Look specifically for the final JSON array after reasoning
            var finalJsonPatterns = new[]
            {
                // Pattern: "Here's the final JSON:" or similar
                @"(?:final|here's|here is|result|output).*?json.*?:?\s*(\[[\s\S]*?\])",
                // Pattern: JSON array at the very end of response
                @"(\[[\s\S]*?\])\s*$",
                // Pattern: JSON array after "conclusion" or "answer"
                @"(?:conclusion|answer|final answer|result).*?(\[[\s\S]*?\])",
                // Pattern: Last complete JSON array in the response
                @"(\[\s*\{[\s\S]*?\}\s*\])(?![\s\S]*\[)"
            };
            
            foreach (var pattern in finalJsonPatterns)
            {
                var matches = Regex.Matches(aiResponse, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    var lastMatch = matches[matches.Count - 1];
                    var jsonCandidate = lastMatch.Groups[1].Value.Trim();
                    
                    if (jsonCandidate.Length > 50) // Reasonable minimum length
                    {
                        try
                        {
                            using (var doc = JsonDocument.Parse(jsonCandidate))
                            {
                                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                                {
                                    _logger.LogDebug($"Found valid JSON using pattern: {pattern.Substring(0, Math.Min(50, pattern.Length))}...");
                                    return jsonCandidate;
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            continue; // Try next pattern
                        }
                    }
                }
            }
            
            // Try to find all JSON arrays in the text (original approach but more robust)
            var jsonArrayMatches = Regex.Matches(aiResponse, @"(\[[\s\S]*?\])", RegexOptions.Singleline);
            if (jsonArrayMatches.Count > 0)
            {
                // Look for the most complete/final JSON array, starting from the end
                for (int i = jsonArrayMatches.Count - 1; i >= 0; i--)
                {
                    var match = jsonArrayMatches[i];
                    var jsonCandidate = match.Groups[1].Value.Trim();
                    
                    // Skip very short potential matches
                    if (jsonCandidate.Length < 100) continue;
                    
                    // Try to validate this as proper JSON
                    try
                    {
                        using (var doc = JsonDocument.Parse(jsonCandidate))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                            {
                                _logger.LogDebug($"Found valid JSON array at match {i} (length: {jsonCandidate.Length})");
                                return jsonCandidate;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // This match is not valid JSON, continue to previous match
                        continue;
                    }
                }
            }
            
            // Try to find anything that looks like a complete JSON structure with checklist items
            // Look for patterns like: [{"id": "...", "text": "...", ...}] 
            var structuredMatches = Regex.Matches(aiResponse, @"(\[\s*\{[\s\S]*?""text""\s*:\s*""[^""]*""[\s\S]*?\}\s*(?:,\s*\{[\s\S]*?\})*\s*\])", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (structuredMatches.Count > 0)
            {
                var lastStructuredMatch = structuredMatches[structuredMatches.Count - 1];
                var candidate = lastStructuredMatch.Groups[1].Value.Trim();
                
                try
                {
                    using (var doc = JsonDocument.Parse(candidate))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                        {
                            _logger.LogDebug("Found valid JSON using structured pattern matching");
                            return candidate;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Continue to fallback methods
                }
            }
            
            // Last resort: try to extract the largest JSON array from the response
            var allBracketMatches = Regex.Matches(aiResponse, @"(\[[\s\S]{100,}?\])", RegexOptions.Singleline);
            if (allBracketMatches.Count > 0)
            {
                // Get the longest match (most likely to be the complete response)
                var longestMatch = allBracketMatches.Cast<Match>()
                    .OrderByDescending(m => m.Groups[1].Value.Length)
                    .First();
                
                var candidate = longestMatch.Groups[1].Value.Trim();
                
                try
                {
                    using (var doc = JsonDocument.Parse(candidate))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            _logger.LogDebug($"Found valid JSON using longest match strategy (length: {candidate.Length})");
                            return candidate;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Still not valid
                }
            }
            
            _logger.LogWarning($"Could not extract valid JSON from reasoning response of {aiResponse.Length} characters");
            
            // Log a sample of the response for debugging (but don't expose full content in error)
            var sample = aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse;
            _logger.LogDebug($"Response sample: {sample}");
            
            return string.Empty;
        }

        private void ValidateAndCleanupChecklistItems(List<ChecklistItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                
                // Ensure required fields are not empty
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    item.Id = $"item_{i + 1:D3}";
                }
                
                if (string.IsNullOrWhiteSpace(item.Text))
                {
                    item.Text = $"Review item {i + 1}";
                }
                
                // Validate type
                if (!IsValidChecklistItemType(item.Type))
                {
                    item.Type = ChecklistItemType.Checkbox;
                }
                
                // Ensure options is not null
                if (item.Options == null)
                {
                    item.Options = new List<string>();
                }
                
                // Clean up text
                item.Text = item.Text.Trim();
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    item.Description = item.Description.Trim();
                }
            }
        }

        private bool IsValidChecklistItemType(ChecklistItemType type)
        {
            return Enum.IsDefined(typeof(ChecklistItemType), type);
        }

        public async Task<string> ConvertChecklistToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            try
            {
                var checklistJson = JsonSerializer.Serialize(checklistItems, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var systemPrompt = @"You are an expert in creating SurveyJS survey configurations. 
Convert checklist items into a well-structured, user-friendly survey form.
Create appropriate question types and logical groupings.
Generate valid question names (alphanumeric + underscore, start with letter).";

                var userPrompt = $@"Convert this checklist data into a valid SurveyJS JSON configuration.

Checklist items:
{checklistJson}

Survey title: {title}

Requirements:
1. Create a complete SurveyJS survey configuration
2. Use appropriate question types: 'boolean' for checkbox items, 'comment' for text fields, 'dropdown' for multiple choice
3. Group related questions into logical pages (max 10 questions per page)
4. Include proper navigation and completion settings
5. Preserve all checklist item details (text, description, required status)
6. Generate valid question names (no spaces, start with letter, alphanumeric + underscore only)
7. Set up proper survey metadata

Return a complete SurveyJS JSON configuration with these properties:
- title
- description
- showProgressBar: ""top""
- completeText: ""Submit""
- showQuestionNumbers: ""off""
- questionTitleLocation: ""top""
- pages (array of page objects with elements)

Return only the JSON configuration, no additional text or markdown formatting.";

                var requestPayload = new
                {
                    model = _deploymentName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 4000,
                    temperature = 0.3,
                    top_p = 0.8
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var requestUrl = $"{_endpoint}/models/chat/completions?api-version=2024-05-01-preview";
                var response = await _httpClient.PostAsync(requestUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Azure AI Foundry API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Azure AI Foundry API error: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseContent);
                var aiResponse = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    throw new InvalidOperationException("Azure AI Foundry returned empty response");
                }

                // Log the full response for debugging
                _logger.LogInformation($"SurveyJS AI response length: {aiResponse.Length} characters");
                _logger.LogDebug($"SurveyJS AI response (first 1000 chars): {aiResponse.Substring(0, Math.Min(1000, aiResponse.Length))}");

                // Extract JSON from the response using the same robust method as checklist extraction
                string jsonContent = ExtractSurveyJSJsonFromResponse(aiResponse);
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogError($"Could not extract valid JSON from SurveyJS AI response. Response length: {aiResponse.Length}");
                    throw new InvalidOperationException($"Could not extract JSON from SurveyJS AI response.");
                }

                // Validate that it's valid JSON
                try
                {
                    var testParse = JsonDocument.Parse(jsonContent);
                    _logger.LogInformation("Successfully generated SurveyJS configuration using Azure AI Foundry");
                    return jsonContent;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"SurveyJS JSON validation failed. Content: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                    _logger.LogError($"JSON error: {jsonEx.Message}");
                    
                    // Try to clean and repair the JSON
                    string cleanedJson = CleanAndRepairSurveyJSJson(jsonContent);
                    try
                    {
                        var testParse2 = JsonDocument.Parse(cleanedJson);
                        _logger.LogInformation("Successfully generated SurveyJS configuration after JSON repair");
                        return cleanedJson;
                    }
                    catch (JsonException)
                    {
                        throw new InvalidOperationException($"Failed to parse SurveyJS AI response as valid JSON. Content: {jsonContent.Substring(0, Math.Min(300, jsonContent.Length))}", jsonEx);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS using Azure AI Foundry");
                throw;
            }
        }
        
        private string CleanAndRepairJson(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return jsonContent;
            
            _logger.LogDebug("Cleaning and repairing JSON content");
            
            // Remove any trailing text after the last ]
            var lastBracketIndex = jsonContent.LastIndexOf(']');
            if (lastBracketIndex >= 0 && lastBracketIndex < jsonContent.Length - 1)
            {
                jsonContent = jsonContent.Substring(0, lastBracketIndex + 1);
            }
            
            // Try to fix common JSON issues
            jsonContent = jsonContent.Trim();
            
            // Fix unclosed quotes by finding incomplete string values
            jsonContent = Regex.Replace(jsonContent, @"""([^""]*?)$", @"""$1""", RegexOptions.Multiline);
            
            // Fix incomplete objects - if we have an unclosed {, try to close it
            var openBraces = jsonContent.Count(c => c == '{');
            var closeBraces = jsonContent.Count(c => c == '}');
            var missingCloseBraces = openBraces - closeBraces;
            
            if (missingCloseBraces > 0)
            {
                jsonContent += new string('}', missingCloseBraces);
            }
            
            // Ensure the array is properly closed
            if (!jsonContent.TrimEnd().EndsWith(']'))
            {
                jsonContent = jsonContent.TrimEnd().TrimEnd(',') + "]";
            }
            
            return jsonContent;
        }
        
        private List<ChecklistItem> ConvertStringArrayToChecklistItems(string[] stringArray)
        {
            _logger.LogInformation($"Converting {stringArray.Length} strings to ChecklistItem objects");
            
            var checklistItems = new List<ChecklistItem>();
            
            for (int i = 0; i < stringArray.Length; i++)
            {
                var text = stringArray[i]?.Trim();
                if (string.IsNullOrEmpty(text)) continue;
                
                var item = new ChecklistItem
                {
                    Id = $"item_{i + 1:D3}",
                    Text = text,
                    Description = $"Extracted from document: {text}",
                    Type = ChecklistItemType.Checkbox,
                    IsRequired = false,
                    Options = new List<string>()
                };
                
                checklistItems.Add(item);
            }
            
            _logger.LogInformation($"Successfully converted {checklistItems.Count} strings to ChecklistItem objects");
            return checklistItems;
        }
        
        private string ExtractSurveyJSJsonFromResponse(string aiResponse)
        {
            _logger.LogDebug($"Extracting SurveyJS JSON from AI response (length: {aiResponse.Length})");
            
            // For SurveyJS, we're looking for a JSON object (not array)
            
            // First try to find JSON in markdown code blocks
            var markdownMatches = Regex.Matches(aiResponse, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (markdownMatches.Count > 0)
            {
                // Return the last (most likely final) JSON block
                var lastMatch = markdownMatches[markdownMatches.Count - 1];
                _logger.LogDebug("Found SurveyJS JSON in markdown code block");
                return lastMatch.Groups[1].Value.Trim();
            }
            
            // Try to find all JSON objects in the text
            var jsonObjectMatches = Regex.Matches(aiResponse, @"(\{[\s\S]*?\})", RegexOptions.Singleline);
            if (jsonObjectMatches.Count > 0)
            {
                // Look for the most complete/final JSON object
                for (int i = jsonObjectMatches.Count - 1; i >= 0; i--)
                {
                    var match = jsonObjectMatches[i];
                    var jsonCandidate = match.Groups[1].Value.Trim();
                    
                    // Skip small objects that are likely not the main response
                    if (jsonCandidate.Length < 50) continue;
                    
                    // Try to validate this as proper JSON
                    try
                    {
                        using (var doc = JsonDocument.Parse(jsonCandidate))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                _logger.LogDebug($"Found valid SurveyJS JSON object at match {i}");
                                return jsonCandidate;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // This match is not valid JSON, continue to previous match
                        continue;
                    }
                }
            }
            
            // Final fallback: try to extract from first { to last }
            var startIndex = aiResponse.IndexOf('{');
            var endIndex = aiResponse.LastIndexOf('}');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var candidate = aiResponse.Substring(startIndex, endIndex - startIndex + 1);
                
                // Try to validate this as JSON
                try
                {
                    using (var doc = JsonDocument.Parse(candidate))
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            _logger.LogDebug("Found valid SurveyJS JSON using fallback brace extraction");
                            return candidate;
                        }
                    }
                }
                catch (JsonException)
                {
                    // This is not valid JSON
                }
            }
            
            _logger.LogWarning("Could not extract valid SurveyJS JSON from AI response");
            return string.Empty;
        }
        
        private string CleanAndRepairSurveyJSJson(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return jsonContent;
            
            _logger.LogDebug("Cleaning and repairing SurveyJS JSON content");
            
            // Remove any trailing text after the last }
            var lastBraceIndex = jsonContent.LastIndexOf('}');
            if (lastBraceIndex >= 0 && lastBraceIndex < jsonContent.Length - 1)
            {
                jsonContent = jsonContent.Substring(0, lastBraceIndex + 1);
            }
            
            // Try to fix common JSON issues for objects
            jsonContent = jsonContent.Trim();
            
            // Fix unclosed quotes by finding incomplete string values
            jsonContent = Regex.Replace(jsonContent, @"""([^""]*?)$", @"""$1""", RegexOptions.Multiline);
            
            // Fix incomplete objects - if we have an unclosed {, try to close it
            var openBraces = jsonContent.Count(c => c == '{');
            var closeBraces = jsonContent.Count(c => c == '}');
            var missingCloseBraces = openBraces - closeBraces;
            
            if (missingCloseBraces > 0)
            {
                jsonContent += new string('}', missingCloseBraces);
            }
            
            // Remove trailing commas before closing braces
            jsonContent = Regex.Replace(jsonContent, @",(\s*})", "$1");
            
            return jsonContent;
        }
    }
    
    public class ChecklistItemTypeConverter : JsonConverter<ChecklistItemType>
    {
        public override ChecklistItemType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return ChecklistItemType.Checkbox;
                
            // Handle common variations and typos
            return value.ToLowerInvariant() switch
            {
                "text" or "textbox" or "textfield" or "input" => ChecklistItemType.Text,
                "boolean" or "bool" or "yes/no" or "yesno" => ChecklistItemType.Boolean,
                "radiogroup" or "radio" or "radiobutton" or "radio group" => ChecklistItemType.RadioGroup,
                "checkbox" or "check" or "checkboxes" => ChecklistItemType.Checkbox,
                "dropdown" or "select" or "choice" or "combobox" => ChecklistItemType.Dropdown,
                "comment" or "note" or "description" or "info" => ChecklistItemType.Comment,
                _ => ChecklistItemType.Checkbox // Default fallback
            };
        }

        public override void Write(Utf8JsonWriter writer, ChecklistItemType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
