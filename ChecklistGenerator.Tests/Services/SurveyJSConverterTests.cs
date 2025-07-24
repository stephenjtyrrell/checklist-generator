using ChecklistGenerator.Models;
using ChecklistGenerator.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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
            
            // Verify it's valid JSON
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("title").GetString().Should().Be("Test Survey");
            deserializedResult.GetProperty("description").GetString().Should().Be("This survey was generated from an Excel document checklist");
        }

        [Fact]
        public void ConvertToSurveyJS_SinglePage_ShouldUseElementsFormat()
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
                },
                new ChecklistItem
                {
                    Id = "item2",
                    Text = "Test question 2",
                    Type = ChecklistItemType.Text,
                    IsRequired = false
                }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Test Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("elements").GetArrayLength().Should().Be(2);
            
            var firstElement = deserializedResult.GetProperty("elements")[0];
            firstElement.GetProperty("name").GetString().Should().Be("item1");
            firstElement.GetProperty("title").GetString().Should().Be("Test question 1");
            firstElement.GetProperty("type").GetString().Should().Be("boolean");
            firstElement.GetProperty("isRequired").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public void ConvertToSurveyJS_MultiPage_ShouldUsePagesFormat()
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>();
            for (int i = 1; i <= 15; i++)
            {
                checklistItems.Add(new ChecklistItem
                {
                    Id = $"item{i}",
                    Text = $"Test question {i}",
                    Type = ChecklistItemType.Boolean,
                    IsRequired = i % 2 == 0
                });
            }

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems, "Large Survey");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            deserializedResult.GetProperty("pages").GetArrayLength().Should().Be(2); // 15 items = 2 pages (10 + 5)
            
            var firstPage = deserializedResult.GetProperty("pages")[0];
            firstPage.GetProperty("elements").GetArrayLength().Should().Be(10);
            firstPage.GetProperty("name").GetString().Should().Be("page_1");
            firstPage.GetProperty("title").GetString().Should().Be("Section 1");
            
            var secondPage = deserializedResult.GetProperty("pages")[1];
            secondPage.GetProperty("elements").GetArrayLength().Should().Be(5);
            secondPage.GetProperty("name").GetString().Should().Be("page_2");
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

        [Theory]
        [InlineData("Test Question", "Test_Question")]
        [InlineData("Question with spaces", "Question_with_spaces")]
        [InlineData("Question-with-dashes", "Question_with_dashes")]
        [InlineData("Question (with) parentheses", "Question__with__parentheses")]
        [InlineData("123 Question", "q_123_Question")]
        [InlineData("", "question_")]
        public void ConvertToSurveyJS_NameGeneration_ShouldCreateValidNames(string input, string expectedStart)
        {
            // Arrange
            var checklistItems = new List<ChecklistItem>
            {
                new ChecklistItem { Id = input, Text = "Test question", Type = ChecklistItemType.Boolean }
            };

            // Act
            var result = _converter.ConvertToSurveyJS(checklistItems);

            // Assert
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            var elementName = deserializedResult.GetProperty("elements")[0].GetProperty("name").GetString();
            
            if (string.IsNullOrEmpty(input))
            {
                elementName.Should().StartWith("question_");
            }
            else
            {
                elementName.Should().StartWith(expectedStart);
            }
        }

        [Fact]
        public void ConvertToSurveyJS_AllChecklistTypes_ShouldConvertToBooleanElements()
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
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            var elements = deserializedResult.GetProperty("elements");
            
            for (int i = 0; i < elements.GetArrayLength(); i++)
            {
                elements[i].GetProperty("type").GetString().Should().Be("boolean");
            }
        }

        [Fact]
        public void ConvertToSurveyJS_WithDescriptions_ShouldIncludeDescriptions()
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
            var deserializedResult = JsonSerializer.Deserialize<JsonElement>(result);
            var element = deserializedResult.GetProperty("elements")[0];
            element.GetProperty("description").GetString().Should().Be("This is a test description");
        }
    }
}
