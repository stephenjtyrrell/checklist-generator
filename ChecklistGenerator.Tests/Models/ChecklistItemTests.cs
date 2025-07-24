using ChecklistGenerator.Models;
using FluentAssertions;
using Xunit;

namespace ChecklistGenerator.Tests.Models
{
    public class ChecklistItemTests
    {
        [Fact]
        public void ChecklistItem_DefaultValues_ShouldBeInitialized()
        {
            // Arrange & Act
            var item = new ChecklistItem();

            // Assert
            item.Id.Should().BeEmpty();
            item.Text.Should().BeEmpty();
            item.Type.Should().Be(ChecklistItemType.Text);
            item.Options.Should().NotBeNull().And.BeEmpty();
            item.IsRequired.Should().BeFalse();
            item.Description.Should().BeEmpty();
        }

        [Fact]
        public void ChecklistItem_SetProperties_ShouldRetainValues()
        {
            // Arrange
            var item = new ChecklistItem();
            var options = new List<string> { "Option1", "Option2" };

            // Act
            item.Id = "test-id";
            item.Text = "Test question";
            item.Type = ChecklistItemType.RadioGroup;
            item.Options = options;
            item.IsRequired = true;
            item.Description = "Test description";

            // Assert
            item.Id.Should().Be("test-id");
            item.Text.Should().Be("Test question");
            item.Type.Should().Be(ChecklistItemType.RadioGroup);
            item.Options.Should().BeEquivalentTo(options);
            item.IsRequired.Should().BeTrue();
            item.Description.Should().Be("Test description");
        }

        [Theory]
        [InlineData(ChecklistItemType.Text)]
        [InlineData(ChecklistItemType.Boolean)]
        [InlineData(ChecklistItemType.RadioGroup)]
        [InlineData(ChecklistItemType.Checkbox)]
        [InlineData(ChecklistItemType.Dropdown)]
        [InlineData(ChecklistItemType.Comment)]
        public void ChecklistItemType_AllValues_ShouldBeValid(ChecklistItemType type)
        {
            // Arrange & Act
            var item = new ChecklistItem { Type = type };

            // Assert
            item.Type.Should().Be(type);
        }
    }
}
