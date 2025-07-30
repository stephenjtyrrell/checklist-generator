using ChecklistGenerator.Models;

namespace ChecklistGenerator.Services
{
    public interface IAzureDocumentIntelligenceService
    {
        Task<List<ChecklistItem>> ProcessDocumentAsync(Stream documentStream, string fileName);
    }
}
