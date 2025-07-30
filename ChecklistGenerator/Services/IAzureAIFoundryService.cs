using ChecklistGenerator.Models;

namespace ChecklistGenerator.Services
{
    public interface IAzureAIFoundryService
    {
        Task<List<ChecklistItem>> ProcessDocumentAsync(Stream documentStream, string fileName);
        Task<List<ChecklistItem>> ConvertDocumentToChecklistAsync(string documentContent, string fileName = "");
        Task<string> ExtractTextFromDocumentAsync(Stream documentStream, string fileName);
        Task<string> ConvertChecklistToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey");
    }
}
