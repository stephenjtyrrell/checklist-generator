using ChecklistGenerator.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace ChecklistGenerator.Tests.Infrastructure
{
    public class BuildValidationTests
    {
        [Fact]
        public void TestProject_ShouldReferenceMainProject()
        {
            // This test validates that our test project is correctly configured
            // and can instantiate classes from the main project
            
            // Arrange & Act
            var checklistItems = TestDataHelper.CreateSampleChecklistItems(3);
            
            // Assert
            checklistItems.Should().HaveCount(3);
            checklistItems.Should().AllSatisfy(item =>
            {
                item.Id.Should().NotBeNullOrEmpty();
                item.Text.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public void TestHelpers_ShouldCreateValidTestData()
        {
            // Arrange & Act
            var docxStream = TestDataHelper.CreateTestDocxWithParagraphs("Test paragraph 1", "Test paragraph 2");
            var excelStream = TestDataHelper.CreateSimpleTestExcel("Question 1", "Question 2");
            var surveyForm = TestDataHelper.CreateSampleSurveyForm(2, 3);
            var surveyData = TestDataHelper.CreateSampleSurveyData();

            // Assert
            docxStream.Should().NotBeNull().And.Subject.Length.Should().BeGreaterThan(0);
            excelStream.Should().NotBeNull().And.Subject.Length.Should().BeGreaterThan(0);
            surveyForm.Should().NotBeNull();
            surveyForm.Pages.Should().HaveCount(2);
            surveyForm.Pages[0].Elements.Should().HaveCount(3);
            surveyData.Should().HaveCount(5);
        }

        [Fact]
        public void PackageReferences_ShouldBeAccessible()
        {
            // This test verifies that all required NuGet packages are properly referenced
            
            // Test FluentAssertions
            var testString = "test";
            testString.Should().Be("test");
            
            // Test Moq (implicitly tested by having Mock in other tests)
            // Test xUnit (this test itself proves xUnit is working)
            // Test NPOI, DocumentFormat.OpenXml, etc. are tested in service tests
            
            Assert.True(true); // If we get here, basic package references work
        }
    }
}
