using ChecklistGenerator.Models;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        private readonly OpenRouterService _openRouterService;
        private readonly ILogger<SurveyJSConverter> _logger;

        public SurveyJSConverter(OpenRouterService openRouterService, ILogger<SurveyJSConverter> logger)
        {
            _openRouterService = openRouterService;
            _logger = logger;
        }

        public async Task<string> ConvertToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            try
            {
                _logger.LogInformation($"Converting {checklistItems.Count} checklist items to SurveyJS using OpenRouter AI");
                
                var surveyJson = await _openRouterService.ConvertChecklistToSurveyJSAsync(checklistItems, title);
                
                // If AI returns empty or null, use fallback
                if (string.IsNullOrWhiteSpace(surveyJson))
                {
                    _logger.LogWarning("AI service returned empty result, using fallback");
                    return CreateFallbackSurvey(checklistItems, title);
                }
                
                _logger.LogInformation("Successfully converted checklist to SurveyJS using AI");
                return surveyJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS using OpenRouter AI, using fallback");
                return CreateFallbackSurvey(checklistItems, title);
            }
        }

        private string CreateFallbackSurvey(List<ChecklistItem> checklistItems, string title)
        {
            var fallbackSurvey = new
            {
                title = title,
                description = "AI service unavailable - generated using fallback method",
                pages = new[]
                {
                    new
                    {
                        name = "page1",
                        elements = checklistItems.Select((item, index) => new
                        {
                            type = item.Type == ChecklistItemType.Boolean ? "boolean" : "text",
                            name = $"item_{index}",
                            title = item.Text,
                            isRequired = item.IsRequired
                        }).ToArray()
                    }
                }
            };

            return System.Text.Json.JsonSerializer.Serialize(fallbackSurvey, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        // Keep the synchronous version for backward compatibility but use async internally
        public string ConvertToSurveyJS(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            return ConvertToSurveyJSAsync(checklistItems, title).GetAwaiter().GetResult();
        }
    }
}
