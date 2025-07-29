using ChecklistGenerator.Models;
using System.Text.Json;
using System.Text;

namespace ChecklistGenerator.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentException("GeminiApiKey not configured");
        }

        public async Task<List<ChecklistItem>> ConvertDocumentToChecklistAsync(string documentContent, string fileName = "")
        {
            try
            {
                var prompt = $@"
Analyze this document content and convert it into a structured checklist. Extract actionable items, requirements, and compliance points.

Document filename: {fileName}

Document content:
{documentContent}

Please return a JSON array of checklist items with the following structure:
[
  {{
    ""id"": ""unique_identifier"",
    ""text"": ""Clear, actionable checklist item"",
    ""description"": ""Additional context or explanation (optional)"",
    ""type"": ""Checkbox"", // Always use 'Checkbox' for actionable items, 'Comment' for informational sections
    ""isRequired"": true/false, // true for mandatory items, false for optional
    ""options"": []
  }}
]

Guidelines:
1. Convert procedural text into actionable checklist items
2. Identify mandatory vs optional requirements
3. Create clear, specific action items
4. Preserve important regulatory or compliance requirements
5. Make items specific and measurable where possible
6. Use 'Comment' type only for section headers or explanatory text
7. Ensure each item is independently actionable
8. For boolean/yes-no items, leave options array empty

Return only the JSON array, no additional text or formatting.";

                var response = await CallGeminiApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                var checklistItems = JsonSerializer.Deserialize<List<ChecklistItem>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Generated {checklistItems?.Count ?? 0} checklist items using Gemini AI");
                return checklistItems ?? new List<ChecklistItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting document to checklist using Gemini AI");
                
                // Return a fallback item indicating the error
                return new List<ChecklistItem>
                {
                    new ChecklistItem
                    {
                        Id = "gemini_conversion_error",
                        Text = "Failed to process document with AI",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = $"Error: {ex.Message}. Please check the document format and try again."
                    }
                };
            }
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

                var prompt = $@"
Convert this checklist data into a valid SurveyJS JSON configuration.

Checklist items:
{checklistJson}

Survey title: {title}

Requirements:
1. Create a complete SurveyJS survey configuration
2. Use appropriate question types: 'boolean' for checkbox items, 'comment' for informational sections
3. Group related questions into logical pages (max 10 questions per page)
4. Include proper navigation and completion settings
5. Preserve all checklist item details (text, description, required status)
6. Generate valid question names (no spaces, start with letter, alphanumeric + underscore only)
7. Set up proper survey metadata (title, description, progress bar, etc.)

Return a complete SurveyJS JSON configuration that can be directly used in SurveyJS Creator.
Include these properties:
- title
- description
- showProgressBar: ""top""
- completeText: ""Submit""
- showQuestionNumbers: ""off""
- questionTitleLocation: ""top""
- pages (array of page objects with elements)

Return only the JSON configuration, no additional text or formatting.";

                var response = await CallGeminiApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                // Validate that it's valid JSON
                var testParse = JsonDocument.Parse(jsonContent);
                
                _logger.LogInformation("Successfully generated SurveyJS configuration using Gemini AI");
                return jsonContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS using Gemini AI");
                
                // Return a basic fallback survey
                var fallbackSurvey = new
                {
                    title = title,
                    description = "Survey generation failed",
                    showProgressBar = "top",
                    completeText = "Submit",
                    showQuestionNumbers = "off",
                    questionTitleLocation = "top",
                    pages = new[]
                    {
                        new
                        {
                            name = "error_page",
                            title = "Error",
                            elements = new[]
                            {
                                new
                                {
                                    type = "comment",
                                    name = "error_message",
                                    title = "Survey Generation Error",
                                    description = $"Failed to generate survey: {ex.Message}"
                                }
                            }
                        }
                    }
                };

                return JsonSerializer.Serialize(fallbackSurvey, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
        }

        public async Task<List<ChecklistItem>> EnhanceChecklistAsync(List<ChecklistItem> existingItems)
        {
            try
            {
                var checklistJson = JsonSerializer.Serialize(existingItems, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var prompt = $@"
Enhance this existing checklist by improving clarity, adding missing steps, and ensuring completeness.

Current checklist:
{checklistJson}

Please:
1. Review each item for clarity and actionability
2. Identify any logical gaps or missing steps
3. Improve wording to be more specific and measurable
4. Add relevant sub-items or dependencies where appropriate
5. Ensure proper categorization and prioritization
6. Maintain the same JSON structure
7. Preserve all existing item IDs
8. Add new items with unique IDs if needed

Return the enhanced checklist as a JSON array with the same structure as the input.
Return only the JSON array, no additional text or formatting.";

                var response = await CallGeminiApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                var enhancedItems = JsonSerializer.Deserialize<List<ChecklistItem>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Enhanced checklist from {existingItems.Count} to {enhancedItems?.Count ?? 0} items using Gemini AI");
                return enhancedItems ?? existingItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing checklist using Gemini AI");
                return existingItems; // Return original items if enhancement fails
            }
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 8192,
                }
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Extract the generated text from the Gemini response
            var candidates = responseData.GetProperty("candidates");
            var firstCandidate = candidates[0];
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var firstPart = parts[0];
            var text = firstPart.GetProperty("text").GetString();

            return text ?? string.Empty;
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Clean up the response to ensure it's valid JSON
            var jsonContent = response.Trim();
            
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7);
            }
            if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Substring(3);
            }
            if (jsonContent.EndsWith("```"))
            {
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            }

            return jsonContent.Trim();
        }
    }
}
