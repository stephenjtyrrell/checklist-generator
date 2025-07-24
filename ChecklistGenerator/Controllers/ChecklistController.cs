using Microsoft.AspNetCore.Mvc;
using ChecklistGenerator.Services;
using ChecklistGenerator.Models;

namespace ChecklistGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistController : ControllerBase
    {
        private readonly DocxToExcelConverter _docxToExcelConverter;
        private readonly ExcelProcessor _excelProcessor;
        private readonly SurveyJSConverter _surveyConverter;
        private readonly ILogger<ChecklistController> _logger;
        
        // In-memory storage for Excel files (in production, consider using a cache like Redis)
        private static readonly Dictionary<string, (byte[] Data, string FileName)> _excelCache = new();

        public ChecklistController(
            DocxToExcelConverter docxToExcelConverter,
            ExcelProcessor excelProcessor,
            SurveyJSConverter surveyConverter,
            ILogger<ChecklistController> logger)
        {
            _docxToExcelConverter = docxToExcelConverter;
            _excelProcessor = excelProcessor;
            _surveyConverter = surveyConverter;
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

                if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only .docx files are supported");
                }

                _logger.LogInformation($"Processing file: {file.FileName}");

                List<ChecklistItem> checklistItems = new List<ChecklistItem>();
                byte[] excelBytes = null;
                string downloadFileName = string.Empty;
                
                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        // Convert DOCX to Excel
                        _logger.LogInformation("Converting DOCX to Excel...");
                        Stream excelStream;
                        try
                        {
                            var conversionResult = await _docxToExcelConverter.ConvertDocxToExcelAsync(stream, file.FileName);
                            excelStream = conversionResult.ExcelStream;
                            excelBytes = conversionResult.ExcelBytes;
                            downloadFileName = conversionResult.FileName;
                            _logger.LogInformation($"DOCX to Excel conversion successful. Download filename: {downloadFileName}");
                        }
                        catch (Exception conversionEx)
                        {
                            _logger.LogError(conversionEx, "Failed to convert DOCX to Excel");
                            return BadRequest($"Failed to convert DOCX to Excel: {conversionEx.Message}");
                        }
                        
                        // Process the Excel file to extract checklist items
                        _logger.LogInformation("Processing Excel file to extract checklist items...");
                        try
                        {
                            checklistItems = await _excelProcessor.ProcessExcelAsync(excelStream, file.FileName);
                            _logger.LogInformation($"Extracted {checklistItems.Count} checklist items from Excel");
                        }
                        catch (Exception processingEx)
                        {
                            _logger.LogError(processingEx, "Failed to process Excel file");
                            return BadRequest($"Failed to process Excel file: {processingEx.Message}");
                        }
                        finally
                        {
                            excelStream?.Dispose();
                        }
                    }
                }
                catch (Exception processingEx)
                {
                    _logger.LogError(processingEx, "Error during file processing");
                    return StatusCode(500, $"Error processing file: {processingEx.Message}");
                }

                if (checklistItems.Count == 0)
                {
                    checklistItems.Add(new ChecklistItem
                    {
                        Id = "no_items_found",
                        Text = "No checklist items were found in this document",
                        Type = ChecklistItemType.Comment,
                        IsRequired = false,
                        Description = "The document may not contain recognizable checklist patterns, or may need manual review."
                    });
                }

                _logger.LogInformation($"Extracted {checklistItems.Count} checklist items");

                var surveyJson = _surveyConverter.ConvertToSurveyJS(
                    checklistItems, 
                    Path.GetFileNameWithoutExtension(file.FileName));

                var hasProcessingIssues = checklistItems.Any(item => 
                    item.Id.Contains("error") || 
                    item.Id.Contains("failed") || 
                    item.Id.Contains("limitation"));

                // Store Excel data in memory for potential download
                var downloadId = Guid.NewGuid().ToString();
                _excelCache[downloadId] = (excelBytes, downloadFileName);
                
                // Clean up old entries (keep only the last 10)
                if (_excelCache.Count > 10)
                {
                    var oldestKeys = _excelCache.Keys.Take(_excelCache.Count - 10).ToList();
                    foreach (var key in oldestKeys)
                    {
                        _excelCache.Remove(key);
                    }
                }

                return Ok(new
                {
                    Success = true,
                    FileName = file.FileName,
                    ItemCount = checklistItems.Count,
                    SurveyJS = surveyJson,
                    ExcelDownloadId = downloadId,
                    ExcelFileName = downloadFileName,
                    Message = hasProcessingIssues 
                        ? "Document processed with some limitations. See generated items for details."
                        : "Successfully processed document using DOCX to Excel conversion.",
                    HasIssues = hasProcessingIssues
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

        [HttpGet("downloadExcel/{downloadId}")]
        public IActionResult DownloadExcel(string downloadId)
        {
            try
            {
                if (!_excelCache.TryGetValue(downloadId, out var excelData))
                {
                    return NotFound("Excel file not found or has expired");
                }

                _logger.LogInformation($"Downloading Excel file: {excelData.FileName}");
                
                return File(excelData.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading Excel file");
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
