using ChecklistGenerator.Models;
using System.Text.Json;
using System.Text;

namespace ChecklistGenerator.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://openrouter.ai/api/v1/chat/completions";
        private readonly string _defaultModel;
        
        // Fallback models in order of preference (verified free models on OpenRouter)
        private readonly string[] _fallbackModels = new[]
        {
            "mistralai/mistral-7b-instruct:free",
            "openchat/openchat-7b:free",
            "huggingfaceh4/zephyr-7b-beta:free",
            "google/gemma-7b-it:free",
            "meta-llama/llama-3.2-1b-instruct:free"
        };

        public OpenRouterService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenRouterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenRouterApiKey"] ?? throw new ArgumentException("OpenRouterApiKey not configured");
            
            // Allow configuration of the default model, fallback to free Llama model
            _defaultModel = configuration["OpenRouterModel"] ?? "meta-llama/llama-3.2-3b-instruct:free";
            
            // Set the required headers for OpenRouter
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://checklist.stephentyrrell.ie");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Checklist Generator");
        }

        public virtual async Task<List<ChecklistItem>> ConvertDocumentToChecklistAsync(string documentContent, string fileName = "")
        {
            try
            {
                var prompt = $@"
Analyze this document content and convert it into a structured checklist. Extract the MOST IMPORTANT actionable items, requirements, and compliance points. Focus on quality over quantity.

Document filename: {fileName}

Document content:
{documentContent}

Please return a JSON array of checklist items with the following structure. LIMIT to maximum 50 items to ensure response fits within API limits:
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
9. CRITICAL: Limit to maximum 50 most important items
10. Prioritize regulatory compliance and mandatory requirements

Return only the JSON array, no additional text or formatting.";

                var response = await CallOpenRouterApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                _logger.LogInformation("Raw JSON from OpenRouter: {JsonContent}", jsonContent.Length > 1000 ? jsonContent.Substring(0, 1000) + "..." : jsonContent);
                
                // Validate JSON completeness before parsing
                if (!IsValidCompleteJson(jsonContent))
                {
                    _logger.LogWarning("JSON response appears to be truncated or incomplete. Attempting to repair...");
                    jsonContent = RepairJsonArray(jsonContent);
                }
                
                // Parse as dynamic first to handle enum conversion manually
                var dynamicItems = JsonSerializer.Deserialize<List<dynamic>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                var checklistItems = ConvertDynamicToChecklistItems(dynamicItems ?? new List<dynamic>());

                _logger.LogInformation($"Generated {checklistItems?.Count ?? 0} checklist items using OpenRouter AI");
                return checklistItems ?? new List<ChecklistItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting document to checklist using OpenRouter AI. Exception type: {ExceptionType}, Message: {Message}", ex.GetType().Name, ex.Message);
                
                // Check if this is a rate limiting issue
                if (ex.Message.Contains("rate limit") || ex.Message.Contains("limit_rpm"))
                {
                    _logger.LogWarning("Rate limiting detected, returning rate limit specific error message");
                    return new List<ChecklistItem>
                    {
                        new ChecklistItem
                        {
                            Id = "rate_limit_error",
                            Text = "AI service temporarily unavailable due to high demand",
                            Type = ChecklistItemType.Comment,
                            IsRequired = false,
                            Description = "The free AI models are experiencing high demand. Please try again in a few minutes, or consider upgrading to a paid OpenRouter plan for higher rate limits."
                        }
                    };
                }
                
                // Check if all models failed - use basic fallback
                if (ex.Message.Contains("All models failed"))
                {
                    _logger.LogWarning("All AI models failed, using basic fallback processing");
                    return CreateBasicFallbackChecklist(documentContent, fileName);
                }
                
                // Return a fallback item indicating the error
                return new List<ChecklistItem>
                {
                    new ChecklistItem
                    {
                        Id = "openrouter_conversion_error",
                        Text = $"Failed to process document with AI: {ex.Message}",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = $"Error: {ex.Message}. Please check the document format and try again."
                    }
                };
            }
        }

        public virtual async Task<string> ConvertChecklistToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
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

                var response = await CallOpenRouterApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                // Validate that it's valid JSON
                var testParse = JsonDocument.Parse(jsonContent);
                
                _logger.LogInformation("Successfully generated SurveyJS configuration using OpenRouter AI");
                return jsonContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS using OpenRouter AI. Exception type: {ExceptionType}, Message: {Message}", ex.GetType().Name, ex.Message);
                
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

        public virtual async Task<List<ChecklistItem>> EnhanceChecklistAsync(List<ChecklistItem> existingItems)
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

                var response = await CallOpenRouterApiAsync(prompt);
                var jsonContent = ExtractJsonFromResponse(response);
                
                // Parse as dynamic first to handle enum conversion manually
                var dynamicItems = JsonSerializer.Deserialize<List<dynamic>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                var enhancedItems = ConvertDynamicToChecklistItems(dynamicItems ?? new List<dynamic>());

                _logger.LogInformation($"Enhanced checklist from {existingItems.Count} to {enhancedItems?.Count ?? 0} items using OpenRouter AI");
                return enhancedItems ?? existingItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing checklist using OpenRouter AI. Exception type: {ExceptionType}, Message: {Message}", ex.GetType().Name, ex.Message);
                return existingItems; // Return original items if enhancement fails
            }
        }

        private async Task<string> CallOpenRouterApiAsync(string prompt)
        {
            var models = new[] { _defaultModel }.Concat(_fallbackModels).ToArray();
            
            foreach (var model in models)
            {
                try
                {
                    _logger.LogInformation("Attempting API call with model: {Model}", model);
                    
                    var requestBody = new
                    {
                        model = model,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = prompt
                            }
                        },
                        max_tokens = 4096,
                        temperature = 0.1,
                        top_p = 0.95,
                        frequency_penalty = 0,
                        presence_penalty = 0
                    };

                    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Sending request to OpenRouter API with {ContentLength} characters using model {Model}. Request URL: {Url}", json.Length, model, _baseUrl);
                    
                    var response = await _httpClient.PostAsync(_baseUrl, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Received successful response from OpenRouter API using model {Model}: {ResponseLength} characters", model, responseContent.Length);
                        
                        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        // Extract the generated text from the OpenRouter response
                        var choices = responseData.GetProperty("choices");
                        var firstChoice = choices[0];
                        var message = firstChoice.GetProperty("message");
                        var content_text = message.GetProperty("content").GetString();

                        return content_text ?? string.Empty;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Model {Model} failed with status {StatusCode}: {Error}", model, response.StatusCode, errorContent);
                        
                        // Check if this is a rate limit error and we should try the next model
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                            errorContent.Contains("rate limit") || 
                            errorContent.Contains("limit_rpm"))
                        {
                            _logger.LogInformation("Rate limit encountered with {Model}, waiting 2 seconds before trying next fallback model", model);
                            await Task.Delay(2000); // Reduced delay to 2 seconds
                            continue;
                        }
                        
                        // Check for specific model availability errors
                        if (errorContent.Contains("not found") || 
                            errorContent.Contains("unavailable") || 
                            response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning("Model {Model} not available, trying next fallback immediately", model);
                            continue;
                        }
                        
                        // For other errors, still try fallback models
                        _logger.LogWarning("API error with {Model}, trying next fallback model", model);
                        continue;
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "HTTP request failed for model {Model}, trying next fallback", model);
                    continue;
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "Request timeout for model {Model}, trying next fallback", model);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error with model {Model}, trying next fallback", model);
                    continue;
                }
            }
            
            // If all models fail, throw an exception
            var errorMessage = $"All models failed. Tried: {string.Join(", ", models)}";
            _logger.LogError(errorMessage);
            throw new HttpRequestException($"OpenRouter API error: {errorMessage}");
        }

        private List<ChecklistItem> CreateBasicFallbackChecklist(string documentContent, string fileName)
        {
            _logger.LogWarning("Creating basic fallback checklist without AI processing");
            
            // Simple rule-based extraction as ultimate fallback
            var lines = documentContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line.Trim()))
                .Take(20) // Limit to first 20 meaningful lines
                .ToList();
            
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "fallback_note",
                    Text = "AI processing unavailable - manual review required",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false,
                    Description = "All AI models are currently unavailable. Please review the document manually and create checklist items as needed."
                }
            };
            
            // Add first few lines as basic checklist items
            for (int i = 0; i < Math.Min(lines.Count, 10); i++)
            {
                var line = lines[i].Trim();
                if (line.Length > 10) // Only include meaningful lines
                {
                    checklistItems.Add(new ChecklistItem
                    {
                        Id = $"basic_item_{i + 1}",
                        Text = line.Length > 100 ? line.Substring(0, 97) + "..." : line,
                        Type = ChecklistItemType.Checkbox,
                        IsRequired = false,
                        Description = "Auto-extracted from document - please review and modify as needed"
                    });
                }
            }
            
            return checklistItems;
        }

        private List<ChecklistItem> ConvertDynamicToChecklistItems(List<dynamic> dynamicItems)
        {
            var checklistItems = new List<ChecklistItem>();
            
            foreach (var item in dynamicItems)
            {
                var jsonElement = (JsonElement)item;
                var checklistItem = new ChecklistItem();
                
                if (jsonElement.TryGetProperty("id", out var idProp))
                    checklistItem.Id = idProp.GetString() ?? "";
                    
                if (jsonElement.TryGetProperty("text", out var textProp))
                    checklistItem.Text = textProp.GetString() ?? "";
                    
                if (jsonElement.TryGetProperty("description", out var descProp))
                    checklistItem.Description = descProp.GetString() ?? "";
                    
                if (jsonElement.TryGetProperty("isRequired", out var requiredProp))
                    checklistItem.IsRequired = requiredProp.GetBoolean();
                    
                // Handle type conversion with fallback
                if (jsonElement.TryGetProperty("type", out var typeProp))
                {
                    var typeString = typeProp.GetString()?.ToLowerInvariant() ?? "text";
                    checklistItem.Type = typeString switch
                    {
                        "boolean" => ChecklistItemType.Boolean,
                        "text" => ChecklistItemType.Text,
                        "radiogroup" => ChecklistItemType.RadioGroup,
                        "checkbox" => ChecklistItemType.Checkbox,
                        "dropdown" => ChecklistItemType.Dropdown,
                        "comment" => ChecklistItemType.Comment,
                        _ => ChecklistItemType.Text // Default fallback
                    };
                }
                
                // Handle options array
                if (jsonElement.TryGetProperty("options", out var optionsProp) && optionsProp.ValueKind == JsonValueKind.Array)
                {
                    checklistItem.Options = new List<string>();
                    foreach (var option in optionsProp.EnumerateArray())
                    {
                        checklistItem.Options.Add(option.GetString() ?? "");
                    }
                }
                
                checklistItems.Add(checklistItem);
            }
            
            return checklistItems;
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

        private bool IsValidCompleteJson(string jsonContent)
        {
            try
            {
                // Basic checks for JSON array completeness
                jsonContent = jsonContent.Trim();
                if (!jsonContent.StartsWith("[") || !jsonContent.EndsWith("]"))
                {
                    return false;
                }

                // Count braces and brackets to detect truncation
                int openBraces = 0;
                int closeBraces = 0;
                int openBrackets = 0;
                int closeBrackets = 0;
                bool inString = false;
                bool escaped = false;

                foreach (char c in jsonContent)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\' && inString)
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"' && !escaped)
                    {
                        inString = !inString;
                        continue;
                    }

                    if (!inString)
                    {
                        switch (c)
                        {
                            case '{': openBraces++; break;
                            case '}': closeBraces++; break;
                            case '[': openBrackets++; break;
                            case ']': closeBrackets++; break;
                        }
                    }
                }

                return openBraces == closeBraces && openBrackets == closeBrackets;
            }
            catch
            {
                return false;
            }
        }

        private string RepairJsonArray(string jsonContent)
        {
            try
            {
                // Basic repair for truncated JSON array
                jsonContent = jsonContent.Trim();
                
                // If it doesn't start with [, add it
                if (!jsonContent.StartsWith("["))
                {
                    jsonContent = "[" + jsonContent;
                }

                // Remove any incomplete trailing object and close the array properly
                var lastCompleteObject = jsonContent.LastIndexOf('}');
                if (lastCompleteObject > 0)
                {
                    jsonContent = jsonContent.Substring(0, lastCompleteObject + 1);
                    if (!jsonContent.EndsWith("]"))
                    {
                        jsonContent += "]";
                    }
                }
                else if (!jsonContent.EndsWith("]"))
                {
                    jsonContent += "]";
                }

                return jsonContent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to repair JSON, returning empty array");
                return "[]";
            }
        }
    }
}
