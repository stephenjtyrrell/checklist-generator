# Checklist Generator - Word to SurveyJS Converter

A web application that converts Word document (.docx) checklists into SurveyJS JSON format with streamlined document processing.

## 🌐 Live Application

**Production App (Azure):** https://checklist-generator-1753368404.eastus.azurecontainer.io:5000

*Upload your DOCX files and convert them to interactive SurveyJS forms instantly!*

## Features

- **DOCX Document Support**: Upload modern .docx files for processing
- **Intelligent Content Extraction**: Automatically extract checklist items and questions from complex documents
- **SurveyJS JSON Generation**: Convert extracted content to industry-standard SurveyJS format
- **Interactive Survey Preview**: Real-time preview of generated surveys using actual SurveyJS rendering
- **Web-based Interface**: User-friendly drag-and-drop interface with real-time feedback
- **Survey Response Testing**: Complete and test surveys directly in the application
- **Response Export**: Save and export survey responses in JSON format
- **In-Memory Processing**: Files are processed in memory without local storage, providing secure and efficient processing
- **Optional Excel Download**: Users can optionally download the generated Excel file for reference
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
- **Cloud Deployment**: Hosted on Azure Container Instances with automatic scaling

## Prerequisites

- .NET 9.0 or later (for local development)
- Word documents in .docx format

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
   - Click "Choose File" or drag and drop a Word document (.docx)
   - The application will automatically detect the file format
3. **Content Extraction**: Click "Convert to SurveyJS" to process the document
4. **Review Results**: 
   - View the generated SurveyJS JSON output
   - See processing status and any warnings or notifications
5. **Preview Survey**: Click "Preview Survey Form" to see an interactive preview of your survey
6. **Test Survey**: Complete the survey form to test functionality and user experience
7. **Export Results**: Save survey responses and export them for analysis
8. **Use JSON**: Copy the generated JSON to use in your SurveyJS applications

## Document Processing Workflow

The application uses a sophisticated multi-stage processing workflow:

### Stage 1: DOCX to Excel Conversion
- **DOCX Processing**: Direct processing of modern .docx files
- **In-Memory Excel Generation**: Conversion to structured Excel format for better data extraction
- **Format Validation**: Ensures file integrity and proper structure
- **Optional Download**: Users can optionally download the generated Excel file

### Stage 2: Content Analysis
The application intelligently analyzes Excel data extracted from Word documents for:
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
- Body: Word document file (.docx)

**Response:** 
```json
{
  "success": true,
  "fileName": "document.docx",
  "itemCount": 5,
  "surveyJS": { /* SurveyJS JSON format */ },
  "excelDownloadId": "guid-string",
  "excelFileName": "document_20250724_103254.xlsx",
  "message": "Successfully processed document using DOCX to Excel conversion.",
  "hasIssues": false
}
```

### GET /api/checklist/downloadExcel/{downloadId}
Download the generated Excel file using the download ID from the upload response.

**Parameters:**
- `downloadId`: The download ID returned from the upload response

**Response:** 
- Excel file download (.xlsx format)

**Error Response:**
```json
{
  "success": false,
  "message": "Error description",
  "details": "Additional error information"
}
```

### POST /api/checklist/saveResults
Save survey response data for analysis and record keeping.

**Request:** 
- Content-Type: application/json
- Body: 
```json
{
  "surveyData": {
    "item_1": "Company ABC",
    "item_2": true,
    "item_3": "UCITS"
  },
  "timestamp": "2025-07-23T10:30:00.000Z"
}
```

**Response:** 
```json
{
  "success": true,
  "id": "unique-submission-id",
  "message": "Survey results saved successfully",
  "timestamp": "2025-07-23T10:30:00.000Z"
}
```

### GET /api/checklist/sample
Get a sample SurveyJS JSON for testing purposes.

**Response:** Sample survey JSON in SurveyJS format

## Interactive Survey Features

The application now includes a fully integrated SurveyJS preview system:

### Survey Preview
- **Real-time Rendering**: Generated JSON is rendered using the actual SurveyJS library
- **Interactive Testing**: Complete surveys directly in the application
- **Response Validation**: Test required fields and validation rules
- **Progress Tracking**: Visual progress bar and completion status

### Survey Response Management
- **Response Capture**: Automatically capture and store survey responses
- **JSON Export**: Export responses in structured JSON format
- **Clipboard Integration**: Copy results directly to clipboard
- **File Download**: Download responses as JSON files for analysis

### User Experience Features
- **Form Reset**: Clear and restart surveys for testing
- **Back Navigation**: Switch between JSON view and survey preview
- **Result Display**: Formatted display of survey completion data
- **Error Handling**: Graceful handling of invalid survey JSON

## Document Processing

The application analyzes Word documents for:

- Paragraphs containing questions (text ending with ?, starting with "please", etc.)
- Tables with question/answer pairs
- Checkboxes and form elements
- Numbered or lettered option lists
- Required field indicators (*, "required", "mandatory")

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
- **SurveyJS Library**: For interactive survey rendering and response collection
- **Built-in JSON serialization**: For SurveyJS format generation
- **HTML/CSS/JavaScript frontend**: Responsive web interface with drag-and-drop support
- **CDN Resources**: SurveyJS core and UI libraries loaded from unpkg CDN

## Architecture

The application follows a clean, service-oriented architecture:

### Core Services
- **DocxToExcelConverter**: Converts .docx files to Excel format for structured processing
- **ExcelProcessor**: Processes Excel files and extracts structured content to identify checklist items
- **SurveyJSConverter**: Converts extracted content to SurveyJS JSON format

### Processing Pipeline
1. **File Upload & Validation**: Secure file handling with format detection
2. **Document to Excel Conversion**: Convert DOCX files to Excel format for structured data extraction
3. **Content Analysis**: Intelligent parsing of Excel data to extract questions and form elements
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
│   ├── DocxToExcelConverter.cs      # .docx to Excel conversion service
│   ├── ExcelProcessor.cs            # Excel file parsing and content extraction
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
