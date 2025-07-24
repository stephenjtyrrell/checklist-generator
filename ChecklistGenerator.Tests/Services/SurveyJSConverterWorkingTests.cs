using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class SurveyJSConverterWorkingTests
    {
        private readonly SurveyJSConverter _converter;

        public SurveyJSConverterWorkingTests()
        {
            _converter = new SurveyJSConverter();
        }

        [Fact]
        public void ConvertToSurveyJS_EmptyList_ShouldReturnValidJson()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Verify it's valid JSON
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("title").GetString().Should().Be("Test Survey");
        }

        [Fact]
        public void ConvertToSurveyJS_SingleItem_ShouldUseElementsFormat()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test question 1",
                    Type = ChecklistItemType.Boolean,
                    IsRequired = true
                }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("elements").GetArrayLength().Should().Be(1);
            
            var firstElement = deserializedResult.GetProperty("elements")[0];
            firstElement.GetProperty("title").GetString().Should().Be("Test question 1");
            firstElement.GetProperty("type").GetString().Should().Be("boolean");
            firstElement.GetProperty("isRequired").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ConvertToSurveyJS_MultipleItems_ShouldUsePagesFormat()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();
            for (int i = 1; i <= 15; i++)
            {
                checklistItems.Add(new ChecklistItem
                {
                    Id = $"item{i}",
                    Text = $"Test question {i}",
                    Type = ChecklistItemType.Boolean
                });
            }

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Large Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("pages").GetArrayLength().Should().Be(2); // 15 items = 2 pages
        }

        [Fact]
        public void ConvertToSurveyJS_DefaultTitle_ShouldUseGeneratedSurvey()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem { Id = "item1", Text = "Test question", Type = ChecklistItemType.Boolean }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("title").GetString().Should().Be("Generated Survey");
        }

        [Fact]
        public void ConvertToSurveyJS_ValidJson_ShouldBeWellFormed()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "test_item",
                    Text = "Test Question with \"quotes\" and special chars!",
                    Type = ChecklistItemType.Boolean,
                    Description = "Test description"
                }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Should be valid JSON despite special characters
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("elements").GetArrayLength().Should().Be(1);
        }
    }
}
