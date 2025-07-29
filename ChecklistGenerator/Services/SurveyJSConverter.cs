using ChecklistGenerator.Models;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        private readonly GeminiService _geminiService;
        private readonly ILogger<SurveyJSConverter> _logger;

        public SurveyJSConverter(GeminiService geminiService, ILogger<SurveyJSConverter> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<string> ConvertToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            try
            {
                _logger.LogInformation($"Converting {checklistItems.Count} checklist items to SurveyJS using Gemini AI");
                
                var surveyJson = await _geminiService.ConvertChecklistToSurveyJSAsync(checklistItems, title);
                
                _logger.LogInformation("Successfully converted checklist to SurveyJS using AI");
                return surveyJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS using Gemini AI");
                throw;
            }
        }

        // Keep the synchronous version for backward compatibility but use async internally
        public string ConvertToSurveyJS(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            return ConvertToSurveyJSAsync(checklistItems, title).GetAwaiter().GetResult();
        }
    }
}
