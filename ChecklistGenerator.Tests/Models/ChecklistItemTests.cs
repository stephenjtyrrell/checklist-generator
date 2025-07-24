using ChecklistGenerator.Models;
using FluentAssertions;
using Xunit;

namespace ChecklistGenerator.Tests.Models
{
    public class ChecklistItemTests
    {
        [Fact]
        public void ChecklistItem_DefaultConstructor_ShouldInitializeProperties()
        {
            // Act
            var item = new ChecklistItem();

            // Assert
            item.Id.Should().BeNullOrEmpty();
            item.Text.Should().BeNullOrEmpty();
            item.Type.Should().Be(ChecklistItemType.Text);
            item.IsRequired.Should().BeFalse();
            item.Options.Should().NotBeNull();
            item.Options.Should().BeEmpty();
        }

        [Fact]
        public void ChecklistItem_SetProperties_ShouldStoreValues()
        {
            // Arrange
            var id = "test_id";
            var text = "Test question";
            var type = ChecklistItemType.RadioGroup;
            var isRequired = true;
            var options = new List<string> { "Option 1", "Option 2" };

            // Act
            var item = new ChecklistItem
            {
                Id = id,
                Text = text,
                Type = type,
                IsRequired = isRequired,
                Options = options
            };

            // Assert
            item.Id.Should().Be(id);
            item.Text.Should().Be(text);
            item.Type.Should().Be(type);
            item.IsRequired.Should().Be(isRequired);
            item.Options.Should().BeEquivalentTo(options);
        }

        [Fact]
        public void ChecklistItemType_ShouldHaveAllExpectedValues()
        {
            // Assert
            Enum.IsDefined(typeof(ChecklistItemType), ChecklistItemType.Text).Should().BeTrue();
            Enum.IsDefined(typeof(ChecklistItemType), ChecklistItemType.Boolean).Should().BeTrue();
            Enum.IsDefined(typeof(ChecklistItemType), ChecklistItemType.RadioGroup).Should().BeTrue();
            Enum.IsDefined(typeof(ChecklistItemType), ChecklistItemType.Checkbox).Should().BeTrue();
            Enum.IsDefined(typeof(ChecklistItemType), ChecklistItemType.Comment).Should().BeTrue();
        }

        [Theory]
        [InlineData(ChecklistItemType.Text)]
        [InlineData(ChecklistItemType.Boolean)]
        [InlineData(ChecklistItemType.RadioGroup)]
        [InlineData(ChecklistItemType.Checkbox)]
        [InlineData(ChecklistItemType.Comment)]
        public void ChecklistItem_WithDifferentTypes_ShouldSetCorrectly(ChecklistItemType type)
        {
            // Act
            var item = new ChecklistItem { Type = type };

            // Assert
            item.Type.Should().Be(type);
        }

        [Fact]
        public void ChecklistItem_WithNullOptions_ShouldHandleGracefully()
        {
            // Act
            var item = new ChecklistItem { Options = null! };

            // Assert
            item.Options.Should().BeNull();
        }

        [Fact]
        public void ChecklistItem_WithEmptyOptions_ShouldHandleGracefully()
        {
            // Act
            var item = new ChecklistItem { Options = new List<string>() };

            // Assert
            item.Options.Should().NotBeNull();
            item.Options.Should().BeEmpty();
        }

        [Fact]
        public void ChecklistItem_WithMultipleOptions_ShouldStoreAll()
        {
            // Arrange
            var options = new List<string> { "Option A", "Option B", "Option C", "Option D" };

            // Act
            var item = new ChecklistItem { Options = options };

            // Assert
            item.Options.Should().HaveCount(4);
            item.Options.Should().ContainInOrder("Option A", "Option B", "Option C", "Option D");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Valid ID")]
        [InlineData("item_123")]
        [InlineData("ITEM-ABC")]
        public void ChecklistItem_WithVariousIds_ShouldStoreCorrectly(string id)
        {
            // Act
            var item = new ChecklistItem { Id = id };

            // Assert
            item.Id.Should().Be(id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Short question")]
        [InlineData("This is a very long question text that might be used for complex survey forms and should be handled properly by the ChecklistItem model")]
        public void ChecklistItem_WithVariousTexts_ShouldStoreCorrectly(string text)
        {
            // Act
            var item = new ChecklistItem { Text = text };

            // Assert
            item.Text.Should().Be(text);
        }
    }
}
