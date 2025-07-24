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
                ShowQuestionNumbers = "off", // Turn off automatic numbering to avoid conflicts
                QuestionTitleLocation = "top",
                ShowNavigationButtons = true,
                GoNextPageAutomatic = false,
                ShowCompletedPage = true
            };

            // For small surveys (≤10 questions), use single page format
            if (checklistItems.Count <= 10)
            {
                survey.Elements.AddRange(checklistItems.Select(ConvertChecklistItemToSurveyElement));
            }
            else
            {
                // For larger surveys, use multi-page format
                var pageElements = new List<SurveyJSElement>();
                const int questionsPerPage = 10;

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
                Type = "boolean" // Always use boolean/checkbox controls
            };

            // No need for choices since boolean controls are simple checkboxes
            // The user can check (true) or leave unchecked (false)

            return element;
        }

        private string GenerateValidName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return $"question_{Guid.NewGuid():N[..8]}";

            // Replace invalid characters with underscores and ensure it starts with a letter
            var cleaned = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
            
            // Remove consecutive underscores and trim
            cleaned = Regex.Replace(cleaned, @"_+", "_").Trim('_');
            
            // Ensure it's not empty and doesn't start with a number
            if (string.IsNullOrEmpty(cleaned) || char.IsDigit(cleaned[0]))
                cleaned = "q_" + cleaned;
            
            // If still empty or too short, generate a fallback
            if (string.IsNullOrEmpty(cleaned) || cleaned.Length < 2)
                cleaned = $"question_{Guid.NewGuid():N[..8]}";
            
            return cleaned.ToLower();
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove excessive whitespace and clean up formatting
            var cleaned = Regex.Replace(text, @"\s+", " ");
            cleaned = cleaned.Trim();
            
            // Only remove numbering at the very start if it's clearly separate from content
            // Be more conservative - only remove if followed by space and not part of the content
            cleaned = Regex.Replace(cleaned, @"^(\d+\.)\s+", "");
            cleaned = Regex.Replace(cleaned, @"^(\d+\))\s+", "");
            cleaned = Regex.Replace(cleaned, @"^([a-zA-Z]\.)\s+", "");
            cleaned = Regex.Replace(cleaned, @"^([a-zA-Z]\))\s+", "");
            
            // Only remove bullets if they're clearly formatting, not content
            cleaned = Regex.Replace(cleaned, @"^[•\-\*○●▪▫]\s+", "");
            
            // Ensure proper capitalization only if the first character is definitely lowercase
            if (cleaned.Length > 0 && char.IsLower(cleaned[0]) && 
                !cleaned.StartsWith("(") && !Regex.IsMatch(cleaned, @"^[a-z]\)"))
            {
                cleaned = char.ToUpper(cleaned[0]) + (cleaned.Length > 1 ? cleaned.Substring(1) : "");
            }
            
            return cleaned.Trim();
        }
    }
}
