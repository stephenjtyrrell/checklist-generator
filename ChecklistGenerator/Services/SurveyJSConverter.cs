using ChecklistGenerator.Models;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        private readonly IAzureAIFoundryService _azureAIFoundryService;
        private readonly ILogger<SurveyJSConverter> _logger;

        public SurveyJSConverter(IAzureAIFoundryService azureAIFoundryService, ILogger<SurveyJSConverter> logger)
        {
            _azureAIFoundryService = azureAIFoundryService;
            _logger = logger;
        }

        public async Task<string> ConvertToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            _logger.LogInformation($"Converting {checklistItems.Count} checklist items to SurveyJS using Azure AI Foundry");
            
            var surveyJson = await _azureAIFoundryService.ConvertChecklistToSurveyJSAsync(checklistItems, title);
            
            // If AI returns empty or null, throw an exception
            if (string.IsNullOrWhiteSpace(surveyJson))
            {
                _logger.LogError("AI service returned empty result");
                throw new InvalidOperationException("Azure AI Foundry service returned empty or null response. Unable to convert checklist to SurveyJS format.");
            }
            
            _logger.LogInformation("Successfully converted checklist to SurveyJS using AI");
            return surveyJson;
        }

        // Keep the synchronous version for backward compatibility but use async internally
        public string ConvertToSurveyJS(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            return ConvertToSurveyJSAsync(checklistItems, title).GetAwaiter().GetResult();
        }
    }
}
