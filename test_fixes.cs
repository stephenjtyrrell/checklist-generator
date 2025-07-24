using ChecklistGenerator.Models;
using ChecklistGenerator.Services;

var items = new List<ChecklistItem>
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
    }
};

var converter = new SurveyJSConverter();
var json = converter.ConvertToSurveyJS(items, "Test Survey");

Console.WriteLine("Generated SurveyJS JSON:");
Console.WriteLine(json);
