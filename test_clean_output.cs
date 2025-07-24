using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using System.Text.Json;

Console.WriteLine("=== SurveyJS Conversion Test ===\n");

var testItems = new List<ChecklistItem>
{
    new ChecklistItem
    {
        Id = "item_001",
        Text = "1. What is your full name?",
        Type = ChecklistItemType.Text,
        IsRequired = true,
        Options = new List<string>()
    },
    new ChecklistItem
    {
        Id = "item_002", 
        Text = "2. Do you have a valid driver's license?",
        Type = ChecklistItemType.Boolean,
        IsRequired = false,
        Options = new List<string> { "Yes", "No" }
    },
    new ChecklistItem
    {
        Id = "item_003",
        Text = "3. Select your preferred contact method:",
        Type = ChecklistItemType.RadioGroup,
        IsRequired = true,
        Options = new List<string> { "a) Email", "b) Phone", "c) Text message" }
    },
    new ChecklistItem
    {
        Id = "item_004",
        Text = "4. Check all skills that apply:",
        Type = ChecklistItemType.Checkbox,
        IsRequired = false,
        Options = new List<string> { "• Programming", "• Design", "• Management" }
    }
};

var converter = new SurveyJSConverter();
var json = converter.ConvertToSurveyJS(testItems, "Clean Survey Test");

Console.WriteLine("Generated SurveyJS JSON:");
Console.WriteLine(json);

// Parse and display the cleaned structure
var parsed = JsonSerializer.Deserialize<JsonElement>(json);
Console.WriteLine("\n=== Verification ===");
if (parsed.TryGetProperty("elements", out var elements))
{
    var elementsArray = elements.EnumerateArray();
    int i = 1;
    foreach (var element in elementsArray)
    {
        if (element.TryGetProperty("title", out var title))
        {
            Console.WriteLine($"Question {i}: {title.GetString()}");
        }
        if (element.TryGetProperty("choices", out var choices))
        {
            var choicesArray = choices.EnumerateArray();
            foreach (var choice in choicesArray)
            {
                if (choice.TryGetProperty("text", out var choiceText))
                {
                    Console.WriteLine($"  - {choiceText.GetString()}");
                }
            }
        }
        Console.WriteLine();
        i++;
    }
}
