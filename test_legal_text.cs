using ChecklistGenerator.Models;
using ChecklistGenerator.Services;

Console.WriteLine("=== Legal Text Processing Test ===\n");

var problematicItem = new ChecklistItem
{
    Id = "item_006",
    Text = "(a) for financial instruments that may be held in custody, the depositary shall: (i) hold in custody all financial instruments that may be registered in a financial instruments account opened in the depositary's books and all financial instruments that can be physically delivered to the depositary; and (ii) ensure that all financial instruments that can be registered in a financial instruments account opened in the depositary's books are registered in the depositary's books within segregated accounts in accordance with the principles set out in Article 16 of Commission Directive 2006/73/EC, opened in the name of the UCITS or the management company acting on behalf of the UCITS, so that they can be clearly identified as belonging to the UCITS in accordance with the applicable law at all times",
    Type = ChecklistItemType.RadioGroup, // This was incorrectly identified
    IsRequired = false,
    Options = new List<string> 
    { 
        "pened in the name of the UCITS", 
        "he management company acting on behalf of the UCITS" 
    }
};

var converter = new SurveyJSConverter();
var testItems = new List<ChecklistItem> { problematicItem };
var json = converter.ConvertToSurveyJS(testItems, "Legal Text Test");

Console.WriteLine("Improved output:");
Console.WriteLine(json);

Console.WriteLine("\n=== Expected Behavior ===");
Console.WriteLine("- Long legal text should be treated as comment field or yes/no question");
Console.WriteLine("- Invalid options like 'pened in the name' should be filtered out");
Console.WriteLine("- Should default to Yes/No choices for regulatory compliance questions");
