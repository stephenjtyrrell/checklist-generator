using ChecklistGenerator.Models;
using FluentAssertions;
using Xunit;

namespace ChecklistGenerator.Tests.Models
{
    public class SurveyJSFormTests
    {
        [Fact]
        public void SurveyJSForm_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var form = new SurveyJSForm();

            // Assert
            form.Title.Should().BeEmpty();
            form.Description.Should().BeEmpty();
            form.Elements.Should().NotBeNull().And.BeEmpty();
            form.Pages.Should().NotBeNull().And.BeEmpty();
            form.ShowProgressBar.Should().Be("top");
            form.CompleteText.Should().Be("Complete");
            form.ShowQuestionNumbers.Should().Be("on");
            form.QuestionTitleLocation.Should().Be("top");
            form.ShowNavigationButtons.Should().BeTrue();
            form.GoNextPageAutomatic.Should().BeFalse();
            form.ShowCompletedPage.Should().BeTrue();
        }

        [Fact]
        public void SurveyJSForm_SetProperties_ShouldRetainValues()
        {
            // Arrange
            var form = new SurveyJSForm();
            var elements = new List<SurveyJSElement> { new SurveyJSElement() };
            var pages = new List<SurveyJSPage> { new SurveyJSPage() };

            // Act
            form.Title = "Test Survey";
            form.Description = "Test Description";
            form.Elements = elements;
            form.Pages = pages;
            form.ShowProgressBar = "bottom";
            form.CompleteText = "Submit";
            form.ShowQuestionNumbers = "off";
            form.QuestionTitleLocation = "left";
            form.ShowNavigationButtons = false;
            form.GoNextPageAutomatic = true;
            form.ShowCompletedPage = false;

            // Assert
            form.Title.Should().Be("Test Survey");
            form.Description.Should().Be("Test Description");
            form.Elements.Should().BeEquivalentTo(elements);
            form.Pages.Should().BeEquivalentTo(pages);
            form.ShowProgressBar.Should().Be("bottom");
            form.CompleteText.Should().Be("Submit");
            form.ShowQuestionNumbers.Should().Be("off");
            form.QuestionTitleLocation.Should().Be("left");
            form.ShowNavigationButtons.Should().BeFalse();
            form.GoNextPageAutomatic.Should().BeTrue();
            form.ShowCompletedPage.Should().BeFalse();
        }
    }

    public class SurveyJSPageTests
    {
        [Fact]
        public void SurveyJSPage_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var page = new SurveyJSPage();

            // Assert
            page.Name.Should().BeEmpty();
            page.Title.Should().BeEmpty();
            page.Elements.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SurveyJSPage_SetProperties_ShouldRetainValues()
        {
            // Arrange
            var page = new SurveyJSPage();
            var elements = new List<SurveyJSElement> { new SurveyJSElement() };

            // Act
            page.Name = "page1";
            page.Title = "Page 1";
            page.Elements = elements;

            // Assert
            page.Name.Should().Be("page1");
            page.Title.Should().Be("Page 1");
            page.Elements.Should().BeEquivalentTo(elements);
        }
    }

    public class SurveyJSElementTests
    {
        [Fact]
        public void SurveyJSElement_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var element = new SurveyJSElement();

            // Assert
            element.Type.Should().BeEmpty();
            element.Name.Should().BeEmpty();
            element.Title.Should().BeEmpty();
            element.Description.Should().BeEmpty();
            element.IsRequired.Should().BeFalse();
            element.Choices.Should().BeNull();
            element.InputType.Should().BeEmpty();
            element.MaxLength.Should().Be(0);
            element.Placeholder.Should().BeEmpty();
        }

        [Fact]
        public void SurveyJSElement_SetProperties_ShouldRetainValues()
        {
            // Arrange
            var element = new SurveyJSElement();
            var choices = new List<SurveyJSChoice> { new SurveyJSChoice() };

            // Act
            element.Type = "text";
            element.Name = "question1";
            element.Title = "Question 1";
            element.Description = "Test description";
            element.IsRequired = true;
            element.Choices = choices;
            element.InputType = "email";
            element.MaxLength = 100;
            element.Placeholder = "Enter text here";

            // Assert
            element.Type.Should().Be("text");
            element.Name.Should().Be("question1");
            element.Title.Should().Be("Question 1");
            element.Description.Should().Be("Test description");
            element.IsRequired.Should().BeTrue();
            element.Choices.Should().BeEquivalentTo(choices);
            element.InputType.Should().Be("email");
            element.MaxLength.Should().Be(100);
            element.Placeholder.Should().Be("Enter text here");
        }
    }

    public class SurveyJSChoiceTests
    {
        [Fact]
        public void SurveyJSChoice_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var choice = new SurveyJSChoice();

            // Assert
            choice.Value.Should().BeEmpty();
            choice.Text.Should().BeEmpty();
        }

        [Fact]
        public void SurveyJSChoice_SetProperties_ShouldRetainValues()
        {
            // Arrange
            var choice = new SurveyJSChoice();

            // Act
            choice.Value = "option1";
            choice.Text = "Option 1";

            // Assert
            choice.Value.Should().Be("option1");
            choice.Text.Should().Be("Option 1");
        }
    }
}
