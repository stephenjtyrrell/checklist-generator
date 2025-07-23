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
                ShowProgressBar = true,
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
                IsRequired = item.IsRequired
            };

            switch (item.Type)
            {
                case ChecklistItemType.Text:
                    element.Type = "text";
                    break;

                case ChecklistItemType.Boolean:
                    element.Type = "boolean";
                    break;

                case ChecklistItemType.RadioGroup:
                    element.Type = "radiogroup";
                    element.Choices = item.Options.Select((option, index) => new SurveyJSChoice
                    {
                        Value = (index + 1).ToString(),
                        Text = option
                    }).ToList();
                    break;

                case ChecklistItemType.Checkbox:
                    element.Type = "checkbox";
                    element.Choices = item.Options.Select((option, index) => new SurveyJSChoice
                    {
                        Value = (index + 1).ToString(),
                        Text = option
                    }).ToList();
                    break;

                case ChecklistItemType.Dropdown:
                    element.Type = "dropdown";
                    element.Choices = item.Options.Select((option, index) => new SurveyJSChoice
                    {
                        Value = (index + 1).ToString(),
                        Text = option
                    }).ToList();
                    break;

                case ChecklistItemType.Comment:
                    element.Type = "comment";
                    break;

                default:
                    element.Type = "text";
                    break;
            }

            return element;
        }
    }
}
