using ChecklistGenerator.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace ChecklistGenerator.Tests.Models
{
    public class SurveyJSFormTests
    {
        [Fact]
        public void SurveyJSForm_DefaultConstructor_ShouldInitializeProperties()
        {
            // Act
            var form = new SurveyJSForm();

            // Assert
            form.Title.Should().BeEmpty();
            form.Description.Should().BeEmpty();
            form.ShowProgressBar.Should().Be("top");
            form.ShowQuestionNumbers.Should().Be("on");
            form.CompleteText.Should().Be("Complete");
            form.Pages.Should().NotBeNull();
            form.Pages.Should().BeEmpty();
        }

        [Fact]
        public void SurveyJSForm_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var title = "Test Survey";
            var description = "Test Description";
            var showProgressBar = "bottom";
            var showQuestionNumbers = "off";
            var completeText = "Submit";

            // Act
            var form = new SurveyJSForm
            {
                Title = title,
                Description = description,
                ShowProgressBar = showProgressBar,
                ShowQuestionNumbers = showQuestionNumbers,
                CompleteText = completeText
            };

            // Assert
            form.Title.Should().Be(title);
            form.Description.Should().Be(description);
            form.ShowProgressBar.Should().Be(showProgressBar);
            form.ShowQuestionNumbers.Should().Be(showQuestionNumbers);
            form.CompleteText.Should().Be(completeText);
        }

        [Fact]
        public void SurveyJSForm_WithPages_ShouldStoreCorrectly()
        {
            // Arrange
            var pages = new List<SurveyJSPage>
            {
                new SurveyJSPage { Name = "page1", Title = "Page 1" },
                new SurveyJSPage { Name = "page2", Title = "Page 2" }
            };

            // Act
            var form = new SurveyJSForm { Pages = pages };

            // Assert
            form.Pages.Should().HaveCount(2);
            form.Pages[0].Name.Should().Be("page1");
            form.Pages[1].Name.Should().Be("page2");
        }

        [Fact]
        public void SurveyJSPage_DefaultConstructor_ShouldInitializeProperties()
        {
            // Act
            var page = new SurveyJSPage();

            // Assert
            page.Name.Should().BeNullOrEmpty();
            page.Title.Should().BeNullOrEmpty();
            page.Elements.Should().NotBeNull();
            page.Elements.Should().BeEmpty();
        }

        [Fact]
        public void SurveyJSPage_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var name = "testPage";
            var title = "Test Page Title";

            // Act
            var page = new SurveyJSPage
            {
                Name = name,
                Title = title
            };

            // Assert
            page.Name.Should().Be(name);
            page.Title.Should().Be(title);
        }

        [Fact]
        public void SurveyJSPage_WithElements_ShouldStoreCorrectly()
        {
            // Arrange
            var elements = new List<SurveyJSElement>
            {
                new SurveyJSElement { Type = "text", Name = "element1", Title = "Element 1" },
                new SurveyJSElement { Type = "boolean", Name = "element2", Title = "Element 2" }
            };

            // Act
            var page = new SurveyJSPage { Elements = elements };

            // Assert
            page.Elements.Should().HaveCount(2);
            page.Elements[0].Type.Should().Be("text");
            page.Elements[1].Type.Should().Be("boolean");
        }

        [Fact]
        public void SurveyJSElement_DefaultConstructor_ShouldInitializeProperties()
        {
            // Act
            var element = new SurveyJSElement();

            // Assert
            element.Type.Should().BeNullOrEmpty();
            element.Name.Should().BeNullOrEmpty();
            element.Title.Should().BeNullOrEmpty();
            element.IsRequired.Should().BeFalse();
            element.Choices.Should().BeNull();
        }

        [Fact]
        public void SurveyJSElement_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var type = "radiogroup";
            var name = "testElement";
            var title = "Test Element";
            var isRequired = true;
            var choices = new List<SurveyJSChoice> 
            { 
                new SurveyJSChoice { Value = "choice1", Text = "Choice 1" },
                new SurveyJSChoice { Value = "choice2", Text = "Choice 2" },
                new SurveyJSChoice { Value = "choice3", Text = "Choice 3" }
            };

            // Act
            var element = new SurveyJSElement
            {
                Type = type,
                Name = name,
                Title = title,
                IsRequired = isRequired,
                Choices = choices
            };

            // Assert
            element.Type.Should().Be(type);
            element.Name.Should().Be(name);
            element.Title.Should().Be(title);
            element.IsRequired.Should().Be(isRequired);
            element.Choices.Should().HaveCount(3);
            element.Choices![0].Value.Should().Be("choice1");
            element.Choices[0].Text.Should().Be("Choice 1");
        }

        [Fact]
        public void SurveyJSElement_WithNullChoices_ShouldHandleGracefully()
        {
            // Act
            var element = new SurveyJSElement { Choices = null };

            // Assert
            element.Choices.Should().BeNull();
        }

        [Fact]
        public void SurveyJSElement_WithEmptyChoices_ShouldHandleGracefully()
        {
            // Act
            var element = new SurveyJSElement { Choices = new List<SurveyJSChoice>() };

            // Assert
            element.Choices.Should().NotBeNull();
            element.Choices.Should().BeEmpty();
        }

        [Theory]
        [InlineData("text")]
        [InlineData("boolean")]
        [InlineData("radiogroup")]
        [InlineData("checkbox")]
        [InlineData("comment")]
        public void SurveyJSElement_WithDifferentTypes_ShouldSetCorrectly(string type)
        {
            // Act
            var element = new SurveyJSElement { Type = type };

            // Assert
            element.Type.Should().Be(type);
        }

        [Fact]
        public void SurveyJSChoice_DefaultConstructor_ShouldInitializeProperties()
        {
            // Act
            var choice = new SurveyJSChoice();

            // Assert
            choice.Value.Should().BeEmpty();
            choice.Text.Should().BeEmpty();
        }

        [Fact]
        public void SurveyJSChoice_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var value = "choice_value";
            var text = "Choice Text";

            // Act
            var choice = new SurveyJSChoice
            {
                Value = value,
                Text = text
            };

            // Assert
            choice.Value.Should().Be(value);
            choice.Text.Should().Be(text);
        }

        [Fact]
        public void SurveyJSForm_Serialization_ShouldProduceValidJson()
        {
            // Arrange
            var form = new SurveyJSForm
            {
                Title = "Test Survey",
                Description = "Test Description",
                ShowProgressBar = "bottom",
                ShowQuestionNumbers = "off",
                CompleteText = "Submit",
                Pages = new List<SurveyJSPage>
                {
                    new SurveyJSPage
                    {
                        Name = "page1",
                        Title = "Page 1",
                        Elements = new List<SurveyJSElement>
                        {
                            new SurveyJSElement
                            {
                                Type = "text",
                                Name = "name",
                                Title = "What is your name?",
                                IsRequired = true
                            }
                        }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(form, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            json.Should().NotBeNullOrEmpty();
            
            // Verify we can deserialize it back
            var deserialized = JsonSerializer.Deserialize<SurveyJSForm>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            deserialized.Should().NotBeNull();
            deserialized!.Title.Should().Be("Test Survey");
            deserialized.Pages.Should().HaveCount(1);
            deserialized.Pages[0].Elements.Should().HaveCount(1);
        }
    }
}
