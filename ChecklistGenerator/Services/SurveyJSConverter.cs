using ChecklistGenerator.Models;
using System.Text.Json;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        private readonly ILogger<SurveyJSConverter> _logger;

        public SurveyJSConverter(ILogger<SurveyJSConverter> logger)
        {
            _logger = logger;
        }

        public Task<string> ConvertToSurveyJSAsync(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            try
            {
                _logger.LogInformation($"Converting {checklistItems.Count} checklist items to SurveyJS");
                
                var surveyJson = CreateSurveyJS(checklistItems, title);
                
                _logger.LogInformation("Successfully converted checklist to SurveyJS");
                return Task.FromResult(surveyJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting checklist to SurveyJS, using fallback");
                return Task.FromResult(CreateFallbackSurvey(checklistItems, title));
            }
        }

        private string CreateSurveyJS(List<ChecklistItem> checklistItems, string title)
        {
            var elements = new List<object>();

            foreach (var item in checklistItems)
            {
                switch (item.Type)
                {
                    case ChecklistItemType.Text:
                        elements.Add(new
                        {
                            type = "text",
                            name = item.Id,
                            title = item.Text,
                            description = !string.IsNullOrEmpty(item.Description) ? item.Description : null,
                            isRequired = item.IsRequired,
                            placeholder = GetPlaceholderForTextFields(item.Text)
                        });
                        break;

                    case ChecklistItemType.Boolean:
                    case ChecklistItemType.Checkbox:
                        elements.Add(new
                        {
                            type = "boolean",
                            name = item.Id,
                            title = item.Text,
                            description = !string.IsNullOrEmpty(item.Description) ? item.Description : null,
                            isRequired = item.IsRequired
                        });
                        break;

                    case ChecklistItemType.Comment:
                        elements.Add(new
                        {
                            type = "comment",
                            name = item.Id,
                            title = item.Text,
                            description = !string.IsNullOrEmpty(item.Description) ? item.Description : null,
                            isRequired = item.IsRequired,
                            placeholder = "Enter your response..."
                        });
                        break;

                    case ChecklistItemType.Dropdown:
                    case ChecklistItemType.RadioGroup:
                        var choices = item.Options?.Where(o => !string.IsNullOrWhiteSpace(o))
                            .Select(option => new { value = option.Trim(), text = option.Trim() })
                            .ToArray() ?? new[] { new { value = "yes", text = "Yes" }, new { value = "no", text = "No" } };

                        elements.Add(new
                        {
                            type = item.Type == ChecklistItemType.RadioGroup ? "radiogroup" : "dropdown",
                            name = item.Id,
                            title = item.Text,
                            description = !string.IsNullOrEmpty(item.Description) ? item.Description : null,
                            choices = choices,
                            isRequired = item.IsRequired
                        });
                        break;

                    default:
                        // Fallback to text field for unknown types
                        elements.Add(new
                        {
                            type = "text",
                            name = item.Id,
                            title = item.Text,
                            description = !string.IsNullOrEmpty(item.Description) ? item.Description : null,
                            isRequired = item.IsRequired
                        });
                        break;
                }
            }

            var survey = new
            {
                title = title,
                description = "Interactive form generated from document analysis using Azure AI",
                pages = new[]
                {
                    new
                    {
                        name = "page1",
                        elements = elements.ToArray()
                    }
                },
                showQuestionNumbers = "off",
                showProgressBar = "top",
                completedHtml = "<h3>Thank you for completing the form!</h3><p>All fields have been submitted successfully.</p>",
                showNavigationButtons = true,
                showCompletedPage = true
            };

            return JsonSerializer.Serialize(survey, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private string GetPlaceholderForTextFields(string fieldTitle)
        {
            var title = fieldTitle.ToLowerInvariant();
            
            if (title.Contains("name"))
                return "Enter full name";
            else if (title.Contains("email"))
                return "Enter email address";
            else if (title.Contains("phone"))
                return "Enter phone number";
            else if (title.Contains("address"))
                return "Enter address";
            else if (title.Contains("date"))
                return "MM/DD/YYYY";
            else if (title.Contains("amount") || title.Contains("number"))
                return "Enter number";
            else if (title.Contains("description") || title.Contains("explain"))
                return "Provide detailed information";
            else
                return "Enter your response";
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
