using System.Text.RegularExpressions;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class RegexTests
    {
        [Fact]
        public void TestComplexNumberingRegex()
        {
            var testTitles = new[]
            {
                "3.1 General Provide that:",
                "3.1.1 The Deed is entered into between the Management Company and the Depositary.",
                "3.2 Reuse of assets",
                "3.2.1 Subject to Section 3.2.2",
                "(a) for financial instruments"
            };

            var complexNumberedItems = testTitles.Where(title => 
                Regex.IsMatch(title, @"^\d+\.\d+")
            ).ToList();

            // Uncomment for debugging:
            // Console.WriteLine($"Found {complexNumberedItems.Count} complex numbered items:");
            // foreach (var item in complexNumberedItems)
            // {
            //     Console.WriteLine($"  - '{item}'");
            // }

            Assert.True(complexNumberedItems.Count > 0, "Should find complex numbered items");
        }
    }
}
