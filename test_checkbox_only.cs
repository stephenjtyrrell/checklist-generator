using ChecklistGenerator.Models;
using ChecklistGenerator.Services;

Console.WriteLine("=== Simplified Checkbox-Only Conversion Test ===\n");

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
        Text = "(a) for financial instruments that may be held in custody, the depositary shall: (i) hold in custody all financial instruments",
        Type = ChecklistItemType.RadioGroup,
        IsRequired = false,
        Options = new List<string> { "pened in the name of the UCITS", "he management company acting on behalf" }
    }
};

var converter = new SurveyJSConverter();
var json = converter.ConvertToSurveyJS(testItems, "Checkbox-Only Survey");

Console.WriteLine("Generated SurveyJS JSON (All as checkboxes):");
Console.WriteLine(json);

Console.WriteLine("\n=== Expected Behavior ===");
Console.WriteLine("- All questions converted to simple boolean/checkbox controls");
Console.WriteLine("- Users can check (true) or leave unchecked (false)");
Console.WriteLine("- No complex options or multiple choice - just tickboxes");
Console.WriteLine("- Clean question text without numbering artifacts");
Console.WriteLine("- Perfect for compliance checklists and audits");
