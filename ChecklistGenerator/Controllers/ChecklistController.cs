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
        private readonly IWebHostEnvironment _webHostEnvironment;
        
        // In-memory cache for Excel files (for production, consider Redis)
        private static readonly Dictionary<string, (byte[] Data, string FileName)> _excelCache = new();
        private const int MaxCacheSize = 10;

        public ChecklistController(
            DocxToExcelConverter docxToExcelConverter,
            ExcelProcessor excelProcessor,
            SurveyJSConverter surveyConverter,
            ILogger<ChecklistController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _docxToExcelConverter = docxToExcelConverter;
            _excelProcessor = excelProcessor;
            _surveyConverter = surveyConverter;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndConvert(IFormFile? file)
        {
            try
            {
                if (file?.Length == 0 || file == null)
                {
                    return BadRequest(new { Success = false, Message = "No file uploaded" });
                }

                var fileName = file.FileName;
                if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { Success = false, Message = "Only .docx files are supported" });
                }

                _logger.LogInformation("Processing file: {FileName}", file.FileName);

                List<ChecklistItem> checklistItems;
                byte[] excelBytes;
                string downloadFileName;
                
                using var stream = file.OpenReadStream();
                
                // Convert DOCX to Excel
                var conversionResult = await _docxToExcelConverter.ConvertDocxToExcelAsync(stream, file.FileName ?? "unknown.docx");
                using var excelStream = conversionResult.ExcelStream;
                excelBytes = conversionResult.ExcelBytes;
                downloadFileName = conversionResult.FileName;
                
                // Process Excel to extract checklist items
                checklistItems = await _excelProcessor.ProcessExcelAsync(excelStream, file.FileName ?? "unknown.docx");
                
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

                var surveyJson = await _surveyConverter.ConvertToSurveyJSAsync(
                    checklistItems, 
                    Path.GetFileNameWithoutExtension(file.FileName ?? "Generated Survey"));

                var hasProcessingIssues = checklistItems.Any(item => 
                    item.Id.Contains("error", StringComparison.OrdinalIgnoreCase) || 
                    item.Id.Contains("failed", StringComparison.OrdinalIgnoreCase) || 
                    item.Id.Contains("limitation", StringComparison.OrdinalIgnoreCase));

                // Cache Excel data for download
                var downloadId = CacheExcelFile(excelBytes, downloadFileName);

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
                        : "Successfully processed document using AI-powered conversion.",
                    HasIssues = hasProcessingIssues
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing uploaded file");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error processing file",
                    Error = ex.Message
                });
            }
        }

        private string CacheExcelFile(byte[] excelBytes, string fileName)
        {
            var downloadId = Guid.NewGuid().ToString();
            _excelCache[downloadId] = (excelBytes, fileName);
            
            // Clean up old entries
            if (_excelCache.Count > MaxCacheSize)
            {
                var oldestKeys = _excelCache.Keys.Take(_excelCache.Count - MaxCacheSize).ToList();
                foreach (var key in oldestKeys)
                {
                    _excelCache.Remove(key);
                }
            }
            
            return downloadId;
        }

        [HttpGet("sample")]
        public async Task<IActionResult> GetSampleSurvey()
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

            var surveyJson = await _surveyConverter.ConvertToSurveyJSAsync(sampleItems, "Sample Survey");

            return Ok(new
            {
                Success = true,
                SurveyJS = surveyJson
            });
        }

        [HttpGet("samples")]
        public IActionResult GetSampleDocuments()
        {
            try
            {
                var samplesPath = Path.Combine(_webHostEnvironment.WebRootPath, "samples");
                
                if (!Directory.Exists(samplesPath))
                {
                    return Ok(new { Success = true, Samples = Array.Empty<object>() });
                }

                var sampleFiles = Directory.GetFiles(samplesPath, "*.docx")
                    .Select(filePath =>
                    {
                        var fileName = Path.GetFileName(filePath);
                        var displayName = GenerateDisplayName(fileName);
                        
                        return new
                        {
                            FileName = fileName,
                            DisplayName = displayName,
                            Description = $"Sample document: {displayName}",
                            Icon = "📄",
                            DownloadUrl = $"/samples/{fileName}"
                        };
                    })
                    .OrderBy(f => f.DisplayName)
                    .ToList();

                return Ok(new { Success = true, Samples = sampleFiles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sample documents");
                return StatusCode(500, new { Success = false, Message = "Error retrieving samples", Error = ex.Message });
            }
        }

        private static string GenerateDisplayName(string fileName)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // Handle UCITS specifically
            if (nameWithoutExtension.Contains("ucits", StringComparison.OrdinalIgnoreCase))
            {
                var sectionMatch = System.Text.RegularExpressions.Regex.Match(
                    nameWithoutExtension, @"section(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                return sectionMatch.Success 
                    ? $"UCITS Section {sectionMatch.Groups[1].Value}"
                    : "UCITS Application";
            }
            
            // Default: title case conversion
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                nameWithoutExtension.Replace("-", " ").Replace("_", " "));
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
