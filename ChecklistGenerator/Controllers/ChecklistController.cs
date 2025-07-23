using Microsoft.AspNetCore.Mvc;
using ChecklistGenerator.Services;
using ChecklistGenerator.Models;

namespace ChecklistGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistController : ControllerBase
    {
        private readonly WordDocumentProcessor _wordProcessor;
        private readonly SurveyJSConverter _surveyConverter;
        private readonly DocumentConverterService _documentConverter;
        private readonly ILogger<ChecklistController> _logger;

        public ChecklistController(
            WordDocumentProcessor wordProcessor,
            SurveyJSConverter surveyConverter,
            DocumentConverterService documentConverter,
            ILogger<ChecklistController> logger)
        {
            _wordProcessor = wordProcessor;
            _surveyConverter = surveyConverter;
            _documentConverter = documentConverter;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndConvert(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) &&
                    !file.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only .doc and .docx files are supported");
                }

                _logger.LogInformation($"Processing file: {file.FileName}");

                List<ChecklistItem> checklistItems = new List<ChecklistItem>();
                bool wasConverted = false;
                
                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        Stream processingStream = stream;
                        
                        // Convert .doc to .docx if needed
                        if (file.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation($"Converting .doc file to .docx format: {file.FileName}");
                            try
                            {
                                processingStream = await _documentConverter.ConvertDocToDocxAsync(stream, file.FileName);
                                wasConverted = true;
                                _logger.LogInformation("Conversion successful");
                            }
                            catch (Exception conversionEx)
                            {
                                _logger.LogWarning($"Doc conversion failed, will try direct processing: {conversionEx.Message}");
                                processingStream = stream;
                                stream.Position = 0; // Reset for direct processing
                            }
                        }
                        
                        // Process the document (either original or converted)
                        checklistItems = await _wordProcessor.ProcessWordDocumentAsync(processingStream, file.FileName);
                        
                        // Clean up converted stream if it was created
                        if (wasConverted && processingStream != stream)
                        {
                            processingStream.Dispose();
                        }
                    }
                }
                catch (Exception processingEx)
                {
                    _logger.LogWarning($"Word processing failed, creating fallback response: {processingEx.Message}");
                    
                    // Create a fallback response with the error information
                    checklistItems.Add(new ChecklistItem
                    {
                        Id = "processing_failed",
                        Text = "Failed to process this document",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = $"Error: {processingEx.Message}. This may be due to file corruption, password protection, or unsupported formatting."
                    });

                    if (file.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase))
                    {
                        checklistItems.Add(new ChecklistItem
                        {
                            Id = "doc_format_suggestion",
                            Text = "Consider converting to .docx format",
                            Type = ChecklistItemType.Comment,
                            IsRequired = false,
                            Description = "Legacy .doc files have limited support. Converting to .docx format typically resolves compatibility issues."
                        });
                    }
                }

                _logger.LogInformation($"Extracted {checklistItems.Count} checklist items");

                var surveyJson = _surveyConverter.ConvertToSurveyJS(
                    checklistItems, 
                    Path.GetFileNameWithoutExtension(file.FileName));

                var hasProcessingIssues = checklistItems.Any(item => 
                    item.Id.Contains("error") || 
                    item.Id.Contains("failed") || 
                    item.Id.Contains("limitation"));

                return Ok(new
                {
                    Success = true,
                    FileName = file.FileName,
                    ItemCount = checklistItems.Count,
                    SurveyJS = surveyJson,
                    Message = hasProcessingIssues 
                        ? "Document processed with some limitations. See generated items for details."
                        : wasConverted
                            ? "Successfully converted .doc to .docx format and processed the document."
                            : file.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) 
                                ? "Processed .doc file with basic text extraction. For better results, consider converting to .docx format."
                                : "Successfully processed document.",
                    HasIssues = hasProcessingIssues,
                    WasConverted = wasConverted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded file");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        [HttpGet("sample")]
        public IActionResult GetSampleSurvey()
        {
            var sampleItems = new List<ChecklistItem>
            {
                new ChecklistItem
                {
                    Id = "sample_1",
                    Text = "What is your company name?",
                    Type = ChecklistItemType.Text,
                    IsRequired = true
                },
                new ChecklistItem
                {
                    Id = "sample_2",
                    Text = "Do you have regulatory approval?",
                    Type = ChecklistItemType.Boolean,
                    IsRequired = true
                },
                new ChecklistItem
                {
                    Id = "sample_3",
                    Text = "Select your company type:",
                    Type = ChecklistItemType.RadioGroup,
                    Options = new List<string> { "UCITS", "AIF", "Other" },
                    IsRequired = true
                }
            };

            var surveyJson = _surveyConverter.ConvertToSurveyJS(sampleItems, "Sample Survey");

            return Ok(new
            {
                Success = true,
                SurveyJS = surveyJson
            });
        }

        [HttpPost("saveResults")]
        public IActionResult SaveSurveyResults([FromBody] SaveSurveyResultsRequest request)
        {
            try
            {
                if (request?.SurveyData == null)
                {
                    return BadRequest("No survey data provided");
                }

                // Generate a unique ID for this submission
                var submissionId = Guid.NewGuid().ToString();
                
                _logger.LogInformation($"Survey results saved with ID: {submissionId}");
                _logger.LogInformation($"Survey data: {System.Text.Json.JsonSerializer.Serialize(request.SurveyData)}");

                // In a real application, you would save this to a database
                // For now, we'll just log it and return success
                
                return Ok(new
                {
                    Success = true,
                    Id = submissionId,
                    Message = "Survey results saved successfully",
                    Timestamp = request.Timestamp ?? DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving survey results");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
    }

    public class SaveSurveyResultsRequest
    {
        public Dictionary<string, object> SurveyData { get; set; } = new Dictionary<string, object>();
        public string? Timestamp { get; set; }
    }
}
