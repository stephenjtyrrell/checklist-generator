using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class SurveyJSConverterTests
    {
        private readonly SurveyJSConverter _converter;

        public SurveyJSConverterTests()
        {
            _converter = new SurveyJSConverter(new NullLogger<SurveyJSConverter>());
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_EmptyList_ShouldReturnValidJson()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Test Survey");
            jsonDocument.RootElement.GetProperty("pages").GetArrayLength().Should().Be(1);
            jsonDocument.RootElement.GetProperty("pages")[0].GetProperty("elements").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_WithChecklistItems_ShouldReturnValidSurveyJS()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test checkbox question",
                    Type = ChecklistItemType.Checkbox,
                    IsRequired = true,
                    Description = "Test description"
                },
                new ChecklistItem
                {
                    Id = "item2",
                    Text = "Test comment question",
                    Type = ChecklistItemType.Comment,
                    IsRequired = false
                },
                new ChecklistItem
                {
                    Id = "item3",
                    Text = "Test dropdown question",
                    Type = ChecklistItemType.Dropdown,
                    IsRequired = true,
                    Options = new List<string> { "Option 1", "Option 2", "Option 3" }
                }
            };

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Test Survey");
            jsonDocument.RootElement.GetProperty("description").GetString().Should().Be("Interactive checklist generated from document analysis");
            
            var pages = jsonDocument.RootElement.GetProperty("pages");
            pages.GetArrayLength().Should().Be(1);
            
            var elements = pages[0].GetProperty("elements");
            elements.GetArrayLength().Should().Be(3);
            
            // Check first element (checkbox)
            var firstElement = elements[0];
            firstElement.GetProperty("type").GetString().Should().Be("boolean");
            firstElement.GetProperty("name").GetString().Should().Be("item1");
            firstElement.GetProperty("title").GetString().Should().Be("Test checkbox question");
            firstElement.GetProperty("isRequired").GetBoolean().Should().BeTrue();
            
            // Check second element (comment)
            var secondElement = elements[1];
            secondElement.GetProperty("type").GetString().Should().Be("comment");
            secondElement.GetProperty("name").GetString().Should().Be("item2");
            secondElement.GetProperty("title").GetString().Should().Be("Test comment question");
            secondElement.GetProperty("isRequired").GetBoolean().Should().BeFalse();
            
            // Check third element (dropdown)
            var thirdElement = elements[2];
            thirdElement.GetProperty("type").GetString().Should().Be("dropdown");
            thirdElement.GetProperty("name").GetString().Should().Be("item3");
            thirdElement.GetProperty("title").GetString().Should().Be("Test dropdown question");
            thirdElement.GetProperty("isRequired").GetBoolean().Should().BeTrue();
            thirdElement.GetProperty("choices").GetArrayLength().Should().Be(3);
        }

        [Fact]
        public async Task ConvertToSurveyJSAsync_ShouldIncludeProgressBarAndCompletionHtml()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test question",
                    Type = ChecklistItemType.Checkbox
                }
            };

            // Act
            var result = await _converter.ConvertToSurveyJSAsync(checklistItems, "Progress Test");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("showQuestionNumbers").GetString().Should().Be("off");
            jsonDocument.RootElement.GetProperty("showProgressBar").GetString().Should().Be("top");
            jsonDocument.RootElement.GetProperty("completedHtml").GetString().Should().Contain("Thank you for completing the checklist!");
        }
    }
}
