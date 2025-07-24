# ChecklistGenerator.Tests

This project contains comprehensive unit tests for the ChecklistGenerator application, providing full test coverage across all components.

## Test Structure

### Models Tests
- `ChecklistItemTests.cs` - Tests for the ChecklistItem model and ChecklistItemType enum
- `SurveyJSFormTests.cs` - Tests for all SurveyJS-related models

### Services Tests
- `SurveyJSConverterTests.cs` - Tests for converting checklist items to SurveyJS format
- `DocxToExcelConverterTests.cs` - Tests for DOCX to Excel conversion functionality
- `ExcelProcessorTests.cs` - Tests for Excel file processing and checklist item extraction

### Controllers Tests
- `ChecklistControllerTests.cs` - Unit tests for the API controller with mocked dependencies

### Integration Tests
- `ChecklistControllerIntegrationTests.cs` - End-to-end tests using TestServer

### Configuration Tests
- `StartupConfigurationTests.cs` - Tests for application startup and dependency injection

### Edge Cases
- `EdgeCaseTests.cs` - Tests for handling extreme scenarios, large files, unicode, etc.

### Infrastructure
- `BuildValidationTests.cs` - Tests to validate build configuration and package references

### Test Helpers
- `TestDataHelper.cs` - Utility methods for creating test data and mock objects

## Test Coverage

The test suite provides comprehensive coverage including:

- ✅ All public methods and properties
- ✅ Error handling and edge cases
- ✅ Integration scenarios
- ✅ Configuration validation
- ✅ Large file handling
- ✅ Unicode and special character support
- ✅ API endpoint functionality
- ✅ File upload/download workflows

## Running Tests

### Local Development
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ChecklistControllerTests"

# Run with verbose output
dotnet test --verbosity normal
```

### Coverage Reports
The GitHub Actions workflow automatically generates coverage reports using ReportGenerator:
- HTML reports for detailed analysis
- Cobertura format for CI integration
- Text summary for quick overview

## Test Dependencies

- **xUnit** - Primary testing framework
- **FluentAssertions** - Fluent assertion library for readable tests
- **Moq** - Mocking framework for dependency isolation
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support
- **coverlet.collector** - Code coverage collection

## Writing New Tests

When adding new functionality:

1. Create unit tests for new services/models
2. Add integration tests for new API endpoints
3. Include edge case testing for error scenarios
4. Update test helpers if new test data patterns are needed
5. Maintain high code coverage (target: >90%)

## Test Naming Conventions

- Test class names: `{ClassUnderTest}Tests`
- Test method names: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`
- Integration test names: `{Endpoint}_{Scenario}_{ExpectedResult}`

## Mocking Strategy

- Mock external dependencies (file system, databases, etc.)
- Use real objects for DTOs and simple value objects
- Mock logger interfaces to reduce test noise
- Verify important interactions with mocks
