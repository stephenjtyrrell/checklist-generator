using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Services
{
    public class SurveyJSConverterTests
    {
        private readonly SurveyJSConverter _converter;

        public SurveyJSConverterTests()
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
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Test Survey");
            jsonDocument.RootElement.GetProperty("description").GetString().Should().Contain("generated from an Excel document");
        }

        [Fact]
        public void ConvertToSurveyJS_SingleItem_ShouldCreateSinglePageFormat()
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
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("elements").GetArrayLength().Should().Be(1);
            
            var firstElement = jsonDocument.RootElement.GetProperty("elements")[0];
            firstElement.GetProperty("title").GetString().Should().Be("Test question 1");
            firstElement.GetProperty("type").GetString().Should().Be("boolean");
            firstElement.GetProperty("isRequired").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ConvertToSurveyJS_MultipleItemsSmallSet_ShouldUseSinglePageFormat()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();
            for (int i = 1; i <= 5; i++)
            {
                checklistItems.Add(new ChecklistItem
                {
                    Id = $"item{i}",
                    Text = $"Test question {i}",
                    Type = ChecklistItemType.Boolean
                });
            }

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Small Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("elements").GetArrayLength().Should().Be(5);
        }

        [Fact]
        public void ConvertToSurveyJS_LargeItemSet_ShouldUseMultiPageFormat()
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
            
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("pages").GetArrayLength().Should().Be(2); // 15 items = 2 pages (10 + 5)
            
            var firstPage = jsonDocument.RootElement.GetProperty("pages")[0];
            firstPage.GetProperty("elements").GetArrayLength().Should().Be(10);
            
            var secondPage = jsonDocument.RootElement.GetProperty("pages")[1];
            secondPage.GetProperty("elements").GetArrayLength().Should().Be(5);
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
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("title").GetString().Should().Be("Generated Survey");
        }

        [Fact]
        public void ConvertToSurveyJS_AllItemTypes_ShouldConvertToBooleanType()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem { Id = "text", Text = "Text question", Type = ChecklistItemType.Text },
                new ChecklistItem { Id = "bool", Text = "Boolean question", Type = ChecklistItemType.Boolean },
                new ChecklistItem { Id = "radio", Text = "Radio question", Type = ChecklistItemType.RadioGroup },
                new ChecklistItem { Id = "checkbox", Text = "Checkbox question", Type = ChecklistItemType.Checkbox },
                new ChecklistItem { Id = "dropdown", Text = "Dropdown question", Type = ChecklistItemType.Dropdown },
                new ChecklistItem { Id = "comment", Text = "Comment question", Type = ChecklistItemType.Comment }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            var jsonDocument = JsonDocument.Parse(result);
            var elements = jsonDocument.RootElement.GetProperty("elements");
            
            for (int i = 0; i < elements.GetArrayLength(); i++)
            {
                elements[i].GetProperty("type").GetString().Should().Be("boolean");
            }
        }

        [Fact]
        public void ConvertToSurveyJS_WithDescription_ShouldIncludeDescription()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "item1",
                    Text = "Test question",
                    Type = ChecklistItemType.Boolean,
                    Description = "This is a test description"
                }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            var jsonDocument = JsonDocument.Parse(result);
            var element = jsonDocument.RootElement.GetProperty("elements")[0];
            element.GetProperty("description").GetString().Should().Be("This is a test description");
        }

        [Fact]
        public void ConvertToSurveyJS_ValidJsonStructure_ShouldBeWellFormed()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "test_item",
                    Text = "Test Question with special chars & symbols!",
                    Type = ChecklistItemType.Boolean
                }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Should parse without throwing
            var jsonDocument = JsonDocument.Parse(result);
            jsonDocument.RootElement.GetProperty("elements").GetArrayLength().Should().Be(1);
        }
    }
}
