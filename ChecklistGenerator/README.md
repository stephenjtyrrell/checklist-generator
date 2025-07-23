# Checklist Generator - Word to SurveyJS Converter

A .NET web application that converts Word document checklists into SurveyJS JSON format with automatic legacy document conversion support.

## Features

- **Universal Word Document Support**: Upload both legacy .doc and modern .docx files
- **Automatic Format Conversion**: Legacy .doc files are automatically converted to .docx format for processing
- **Intelligent Content Extraction**: Automatically extract checklist items and questions from complex documents
- **SurveyJS JSON Generation**: Convert extracted content to industry-standard SurveyJS format
- **Web-based Interface**: User-friendly drag-and-drop interface with real-time feedback
- **Comprehensive Question Types**:
  - Text input fields
  - Yes/No (Boolean) questions
  - Multiple choice (Radio groups)
  - Multi-select checkboxes
  - Dropdown selections
- **Smart Processing Features**:
  - Required field detection
  - Question type inference
  - Table data extraction
  - Progress tracking and user feedback

## Prerequisites

- .NET 9.0 or later
- Word documents in .doc or .docx format (both legacy and modern formats supported)

## Installation

1. Clone or download the project
2. Navigate to the ChecklistGenerator directory
3. Run the following commands:

```bash
dotnet restore
dotnet build
dotnet run
```

4. Open your browser and go to `http://localhost:5000` (or the URL shown in the console)

## Usage

1. **Start the Application**: Open the web application in your browser
2. **Upload Document**: 
   - Click "Choose File" or drag and drop a Word document (.doc or .docx)
   - The application will automatically detect the file format
3. **Automatic Processing**: 
   - Legacy .doc files are automatically converted to .docx format
   - Conversion status is displayed with progress feedback
4. **Content Extraction**: Click "Convert to SurveyJS" to process the document
5. **Review Results**: 
   - View the generated SurveyJS JSON output
   - See processing status and any warnings or notifications
6. **Use JSON**: Copy the generated JSON to use in your SurveyJS applications

## Document Processing Workflow

The application uses a sophisticated multi-stage processing workflow:

### Stage 1: Format Detection and Conversion
- **Legacy .doc files**: Automatically converted to .docx using text extraction and OpenXML reconstruction
- **Modern .docx files**: Processed directly without conversion
- **Conversion feedback**: Real-time status updates and warnings for conversion issues

### Stage 2: Content Analysis
The application intelligently analyzes Word documents for:
- **Question Detection**: Paragraphs containing questions (text ending with ?, starting with "please", etc.)
- **Table Processing**: Tables with question/answer pairs and structured data
- **Form Elements**: Checkboxes, form fields, and interactive elements
- **List Recognition**: Numbered or lettered option lists and choice structures
- **Required Fields**: Indicators like *, "required", "mandatory", or similar markers
- **Context Understanding**: Relationships between questions and answer choices

### Stage 3: SurveyJS Conversion
- **Type Inference**: Automatic determination of appropriate question types
- **Structure Optimization**: Logical grouping and ordering of survey elements
- **Validation**: Ensuring generated JSON meets SurveyJS schema requirements

## API Endpoints

### POST /api/checklist/upload
Upload and convert a Word document to SurveyJS format with automatic format detection and conversion.

**Request:** 
- Content-Type: multipart/form-data
- Body: Word document file (.doc or .docx)

**Response:** 
```json
{
  "success": true,
  "message": "Document processed successfully",
  "conversionPerformed": true,  // true if .doc was converted to .docx
  "warnings": ["Any processing warnings"],
  "data": {
    // SurveyJS JSON format
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Error description",
  "details": "Additional error information"
}
```

### GET /api/checklist/sample
Get a sample SurveyJS JSON for testing purposes.

**Response:** Sample survey JSON in SurveyJS format

## Document Processing

The application analyzes Word documents for:

- Paragraphs containing questions (text ending with ?, starting with "please", etc.)
- Tables with question/answer pairs
- Checkboxes and form elements
- Numbered or lettered option lists
- Required field indicators (*, "required", "mandatory")

## Legacy Document Support

The application includes robust support for legacy .doc files:

- **Automatic Detection**: File format is automatically detected upon upload
- **Text Extraction**: Content is extracted from legacy .doc files using advanced parsing
- **Format Conversion**: Extracted content is reconstructed into modern .docx format
- **Preservation**: Original document structure and formatting are maintained where possible
- **Feedback**: Users receive clear status updates during the conversion process
- **Error Handling**: Graceful handling of conversion issues with informative error messages

## SurveyJS Output Format

The generated JSON follows the SurveyJS schema and includes:

- Survey metadata (title, description)
- Question elements with appropriate types
- Answer choices for multiple-choice questions
- Required field indicators
- Progress bar configuration

## Supported Question Types

- **Text**: Open-ended text input
- **Boolean**: Yes/No questions
- **Radio Group**: Single selection from multiple options
- **Checkbox**: Multiple selection from options
- **Dropdown**: Single selection from dropdown list
- **Comment**: Large text area for comments

## Example Output

```json
{
  "title": "Sample Survey",
  "description": "This survey was generated from a Word document checklist",
  "showProgressBar": true,
  "completeText": "Submit",
  "elements": [
    {
      "type": "text",
      "name": "item_1",
      "title": "What is your company name?",
      "isRequired": true
    },
    {
      "type": "boolean",
      "name": "item_2",
      "title": "Do you have regulatory approval?",
      "isRequired": true
    }
  ]
}
```

## Development

The application is built with:

- **ASP.NET Core 9.0**: Modern web framework with high performance
- **DocumentFormat.OpenXml**: For modern .docx document processing and creation
- **NPOI**: For legacy .doc file text extraction and parsing
- **Built-in JSON serialization**: For SurveyJS format generation
- **HTML/CSS/JavaScript frontend**: Responsive web interface with drag-and-drop support

## Architecture

The application follows a clean, service-oriented architecture:

### Core Services
- **DocumentConverterService**: Handles .doc to .docx conversion with text extraction
- **WordDocumentProcessor**: Processes .docx files and extracts structured content
- **SurveyJSConverter**: Converts extracted content to SurveyJS JSON format

### Processing Pipeline
1. **File Upload & Validation**: Secure file handling with format detection
2. **Format Conversion**: Automatic .doc to .docx conversion when needed
3. **Content Extraction**: Intelligent parsing of document structure and content
4. **Data Transformation**: Conversion to SurveyJS-compatible format
5. **Response Generation**: JSON output with comprehensive status information

## File Structure

```
ChecklistGenerator/
├── Controllers/
│   └── ChecklistController.cs        # API endpoints and request handling
├── Models/
│   ├── ChecklistItem.cs             # Data models for extracted content
│   └── SurveyJSForm.cs              # SurveyJS schema models
├── Services/
│   ├── DocumentConverterService.cs  # .doc to .docx conversion service
│   ├── WordDocumentProcessor.cs     # .docx document parsing and extraction
│   └── SurveyJSConverter.cs         # JSON conversion and formatting
├── wwwroot/
│   └── index.html                   # Web interface with drag-and-drop support
├── Program.cs                       # Application configuration and DI setup
└── README.md                        # This documentation
```

## Error Handling

The application includes comprehensive error handling:

- **File Validation**: Checks for valid Word document formats
- **Conversion Errors**: Graceful handling of .doc conversion issues
- **Processing Errors**: Detailed error messages for document parsing problems
- **User Feedback**: Clear status messages and progress indicators
- **Logging**: Comprehensive logging for debugging and monitoring

## Performance Considerations

- **Streaming Processing**: Large documents are processed efficiently
- **Memory Management**: Proper disposal of document resources
- **Async Operations**: Non-blocking file processing operations
- **Error Recovery**: Robust handling of malformed or corrupted documents

## License

This project is open source and available under the MIT License.
