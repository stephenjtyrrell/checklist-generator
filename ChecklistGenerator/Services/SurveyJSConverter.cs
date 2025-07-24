using ChecklistGenerator.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChecklistGenerator.Services
{
    public class SurveyJSConverter
    {
        public string ConvertToSurveyJS(List<ChecklistItem> checklistItems, string title = "Generated Survey")
        {
            var survey = new SurveyJSForm
            {
                Title = title,
                Description = "This survey was generated from an Excel document checklist",
                ShowProgressBar = "top",
                CompleteText = "Submit",
                ShowQuestionNumbers = "on",
                QuestionTitleLocation = "top",
                ShowNavigationButtons = true,
                GoNextPageAutomatic = false,
                ShowCompletedPage = true
            };

            var pageElements = new List<SurveyJSElement>();
            const int questionsPerPage = 10; // Group questions into pages

            for (int i = 0; i < checklistItems.Count; i++)
            {
                var item = checklistItems[i];
                var element = ConvertChecklistItemToSurveyElement(item);
                pageElements.Add(element);

                // Create a new page every 10 questions or at the end
                if ((i + 1) % questionsPerPage == 0 || i == checklistItems.Count - 1)
                {
                    var page = new SurveyJSPage
                    {
                        Name = $"page_{(i / questionsPerPage) + 1}",
                        Title = $"Section {(i / questionsPerPage) + 1}",
                        Elements = new List<SurveyJSElement>(pageElements)
                    };
                    survey.Pages.Add(page);
                    pageElements.Clear();
                }
            }

            // If no pages were created, add all elements to a single page
            if (survey.Pages.Count == 0)
            {
                survey.Elements.AddRange(checklistItems.Select(ConvertChecklistItemToSurveyElement));
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(survey, options);
        }

        private SurveyJSElement ConvertChecklistItemToSurveyElement(ChecklistItem item)
        {
            var element = new SurveyJSElement
            {
                Name = GenerateValidName(item.Id),
                Title = CleanText(item.Text),
                Description = CleanText(item.Description),
                IsRequired = item.IsRequired,
                Type = MapChecklistTypeToSurveyJSType(item.Type)
            };

            // Configure specific properties based on type
            switch (item.Type)
            {
                case ChecklistItemType.Text:
                    ConfigureTextInput(element, item.Text);
                    break;
                    
                case ChecklistItemType.Boolean:
                case ChecklistItemType.RadioGroup:
                case ChecklistItemType.Dropdown:
                    if (item.Options.Any())
                    {
                        element.Choices = item.Options.Select(option => new SurveyJSChoice
                        {
                            Value = GenerateValidValue(option),
                            Text = CleanText(option)
                        }).ToList();
                    }
                    else
                    {
                        // Default Yes/No for boolean
                        element.Choices = new List<SurveyJSChoice>
                        {
                            new SurveyJSChoice { Value = "yes", Text = "Yes" },
                            new SurveyJSChoice { Value = "no", Text = "No" }
                        };
                    }
                    break;
                    
                case ChecklistItemType.Checkbox:
                    if (item.Options.Any())
                    {
                        element.Choices = item.Options.Select(option => new SurveyJSChoice
                        {
                            Value = GenerateValidValue(option),
                            Text = CleanText(option)
                        }).ToList();
                    }
                    break;
                    
                case ChecklistItemType.Comment:
                    element.Type = "comment";
                    element.Placeholder = "Enter your comments here...";
                    break;
            }

            return element;
        }

        private void ConfigureTextInput(SurveyJSElement element, string questionText)
        {
            var lowerText = questionText.ToLower();
            
            // Set input type and constraints based on question content
            if (lowerText.Contains("email"))
            {
                element.InputType = "email";
                element.Placeholder = "Enter email address";
            }
            else if (lowerText.Contains("phone") || lowerText.Contains("telephone"))
            {
                element.InputType = "tel";
                element.Placeholder = "Enter phone number";
            }
            else if (lowerText.Contains("website") || lowerText.Contains("url"))
            {
                element.InputType = "url";
                element.Placeholder = "Enter website URL";
            }
            else if (lowerText.Contains("date"))
            {
                element.InputType = "date";
            }
            else if (lowerText.Contains("time"))
            {
                element.InputType = "time";
            }
            else if (lowerText.Contains("number") || lowerText.Contains("amount") || 
                     lowerText.Contains("quantity") || lowerText.Contains("age"))
            {
                element.InputType = "number";
                element.Placeholder = "Enter number";
            }
            else if (lowerText.Contains("name"))
            {
                element.Placeholder = "Enter name";
                element.MaxLength = 100;
            }
            else if (lowerText.Contains("address"))
            {
                element.Placeholder = "Enter address";
                element.MaxLength = 200;
            }
            else if (lowerText.Contains("description") || lowerText.Contains("explain") || 
                     lowerText.Contains("describe") || lowerText.Contains("details"))
            {
                element.Type = "comment";
                element.Placeholder = "Enter detailed response...";
            }
            else
            {
                element.Placeholder = "Enter your response";
                element.MaxLength = 255;
            }
        }

        private string MapChecklistTypeToSurveyJSType(ChecklistItemType type)
        {
            return type switch
            {
                ChecklistItemType.Text => "text",
                ChecklistItemType.Boolean => "radiogroup",
                ChecklistItemType.RadioGroup => "radiogroup",
                ChecklistItemType.Checkbox => "checkbox",
                ChecklistItemType.Dropdown => "dropdown",
                ChecklistItemType.Comment => "comment",
                _ => "text"
            };
        }

        private string GenerateValidName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return $"question_{Guid.NewGuid():N}";

            // Replace invalid characters with underscores and ensure it starts with a letter
            var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
            if (char.IsDigit(cleaned[0]))
                cleaned = "q_" + cleaned;
            
            return cleaned.ToLower();
        }

        private string GenerateValidValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return $"value_{Guid.NewGuid():N}";

            // Create a clean value identifier
            var cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
            return cleaned.ToLower().Trim('_');
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove excessive whitespace and clean up formatting
            var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            cleaned = cleaned.Trim();
            
            // Remove common artifacts from Excel conversion
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\d+[\.\)]\s*", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[a-zA-Z][\.\)]\s*", "");
            
            return cleaned;
        }
    }
}
