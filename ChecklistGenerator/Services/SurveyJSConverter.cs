using ChecklistGenerator.Models;
using System.Text.Json;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        public string ConvertToSurveyJS(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            var survey = new SurveyJSForm
            {
                Title = title,
                Description = "This survey was generated from a Word document checklist",
                ShowProgressBar = "top",
                CompleteText = "Submit"
            };

            foreach (var item in checklistItems)
            {
                var element = ConvertChecklistItemToSurveyElement(item);
                survey.Elements.Add(element);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            return JsonSerializer.Serialize(survey, options);
        }

        private SurveyJSElement ConvertChecklistItemToSurveyElement(ChecklistItem item)
        {
            var element = new SurveyJSElement
            {
                Name = item.Id,
                Title = item.Text,
                Description = item.Description,
                IsRequired = item.IsRequired,
                Type = "boolean" // All controls are now boolean (Yes/No)
            };

            return element;
        }
    }
}
