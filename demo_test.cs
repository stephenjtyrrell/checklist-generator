using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ChecklistGenerator.Tests
{
    public class ConversionImprovementsDemo
    {
        public static async Task RunDemo()
        {
            // Create logger (simplified for demo)
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<ExcelProcessor>();
            
            var excelProcessor = new ExcelProcessor(logger);
            var surveyConverter = new SurveyJSConverter();

            // Demo data simulating Excel content
            var testQuestions = new List<string>
            {
                "What is your full name?",
                "Do you have a valid driver's license?",
                "What is your email address?",
                "How many years of experience do you have?",
                "Select your preferred contact method: a) Email b) Phone c) Text message",
                "Check all that apply: • Morning person • Night owl • Flexible schedule",
                "When did you start your current job?",
                "Please describe your career goals in detail",
                "Are you willing to relocate? Yes/No",
                "Select from the following departments: Sales, Marketing, Engineering, HR"
            };

            // Convert test questions to ChecklistItems (simulating Excel processing)
            var checklistItems = new List<ChecklistItem>();
            for (int i = 0; i < testQuestions.Count; i++)
            {
                var item = await SimulateExcelProcessing(testQuestions[i], i + 1, excelProcessor);
                if (item != null)
                {
                    checklistItems.Add(item);
                }
            }

            // Convert to SurveyJS
            var surveyJson = surveyConverter.ConvertToSurveyJS(checklistItems, "Demo Survey - Improved Conversion");

            // Display results
            Console.WriteLine("=== IMPROVED EXCEL TO SURVEYJS CONVERSION DEMO ===\n");
            Console.WriteLine($"Processed {checklistItems.Count} questions:\n");

            for (int i = 0; i < checklistItems.Count; i++)
            {
                var item = checklistItems[i];
                Console.WriteLine($"{i + 1}. \"{item.Text}\"");
                Console.WriteLine($"   Type: {item.Type}");
                if (item.Options.Any())
                {
                    Console.WriteLine($"   Options: {string.Join(", ", item.Options)}");
                }
                Console.WriteLine($"   Required: {item.IsRequired}");
                Console.WriteLine();
            }

            Console.WriteLine("=== GENERATED SURVEYJS JSON ===");
            Console.WriteLine(surveyJson);
        }

        private static async Task<ChecklistItem?> SimulateExcelProcessing(string questionText, int itemNumber, ExcelProcessor processor)
        {
            // This simulates what the Excel processor would do with cell content
            // Using reflection to access private method for demo purposes
            var method = typeof(ExcelProcessor).GetMethod("AnalyzeContentForChecklistItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                var task = (Task<ChecklistItem?>)method.Invoke(processor, new object[] { questionText, itemNumber, "" });
                return await task;
            }

            // Fallback manual processing for demo
            return new ChecklistItem
            {
                Id = $"item_{itemNumber:D3}",
                Text = questionText,
                Type = DetermineTypeDemo(questionText),
                IsRequired = questionText.Contains("*") || questionText.ToLower().Contains("required"),
                Options = ExtractOptionsDemo(questionText)
            };
        }

        private static ChecklistItemType DetermineTypeDemo(string text)
        {
            var lowerText = text.ToLower();
            
            if (lowerText.Contains("email"))
                return ChecklistItemType.Text;
            if (lowerText.Contains("name"))
                return ChecklistItemType.Text;
            if (lowerText.Contains("how many") || lowerText.Contains("years"))
                return ChecklistItemType.Text;
            if (lowerText.Contains("when did"))
                return ChecklistItemType.Text;
            if (lowerText.Contains("describe") || lowerText.Contains("detail"))
                return ChecklistItemType.Text;
            if (lowerText.Contains("select your") && (lowerText.Contains("a)") || lowerText.Contains("•")))
                return lowerText.Contains("check all") ? ChecklistItemType.Checkbox : ChecklistItemType.RadioGroup;
            if (lowerText.Contains("select from"))
                return ChecklistItemType.Dropdown;
            if (lowerText.Contains("yes/no") || lowerText.StartsWith("do you") || lowerText.StartsWith("are you"))
                return ChecklistItemType.Boolean;
                
            return ChecklistItemType.Boolean;
        }

        private static List<string> ExtractOptionsDemo(string text)
        {
            var options = new List<string>();
            
            if (text.Contains("a)") && text.Contains("b)"))
            {
                var parts = text.Split(new[] { "a)", "b)", "c)", "d)" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < parts.Length; i++)
                {
                    var option = parts[i].Trim().Split(new[] { "b)", "c)", "d)" }, 2)[0].Trim();
                    if (!string.IsNullOrEmpty(option))
                        options.Add(option);
                }
            }
            else if (text.Contains("•"))
            {
                var parts = text.Split('•', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < parts.Length; i++)
                {
                    var option = parts[i].Trim();
                    if (!string.IsNullOrEmpty(option))
                        options.Add(option);
                }
            }
            else if (text.ToLower().Contains("yes/no"))
            {
                options.AddRange(new[] { "Yes", "No" });
            }
            else if (text.Contains(":") && text.Contains(","))
            {
                var colonIndex = text.IndexOf(':');
                if (colonIndex > 0 && colonIndex < text.Length - 1)
                {
                    var optionsPart = text.Substring(colonIndex + 1).Trim();
                    options.AddRange(optionsPart.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)));
                }
            }
            
            return options;
        }
    }
}
