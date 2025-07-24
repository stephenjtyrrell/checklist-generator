using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ClosedXML.Excel;

namespace ChecklistGenerator.Services
{
    public class DocxToExcelConverter
    {
        private readonly ILogger<DocxToExcelConverter> _logger;

        public DocxToExcelConverter(ILogger<DocxToExcelConverter> logger)
        {
            _logger = logger;
        }

        public async Task<(Stream ExcelStream, byte[] ExcelBytes, string FileName)> ConvertDocxToExcelAsync(Stream docxStream, string originalFileName)
        {
            try
            {
                // Generate filename for download
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
                var safeFileName = string.Join("_", baseFileName.Split(Path.GetInvalidFileNameChars()));
                var downloadFileName = $"{safeFileName}_{timestamp}.xlsx";

                var tempDocxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
                var tempExcelPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

                try
                {
                    // Write DOCX stream to temporary file
                    using (var fileStream = File.Create(tempDocxPath))
                    {
                        docxStream.Position = 0;
                        await docxStream.CopyToAsync(fileStream);
                    }

                    // Run the integrated conversion to temporary Excel file
                    await ConvertDocxToExcel(tempDocxPath, tempExcelPath);

                    // Read the Excel file into memory
                    var excelBytes = await File.ReadAllBytesAsync(tempExcelPath);
                    var excelStream = new MemoryStream(excelBytes);
                    
                    _logger.LogInformation($"Excel conversion completed in memory. Download filename: {downloadFileName}");
                    
                    return (excelStream, excelBytes, downloadFileName);
                }
                finally
                {
                    // Clean up temporary files
                    if (File.Exists(tempDocxPath))
                        File.Delete(tempDocxPath);
                    if (File.Exists(tempExcelPath))
                        File.Delete(tempExcelPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting DOCX to Excel");
                throw new InvalidOperationException($"Failed to convert DOCX to Excel: {ex.Message}", ex);
            }
        }

        private async Task ConvertDocxToExcel(string inputPath, string outputPath)
        {
            await Task.Run(() =>
            {
                var rawRows = new List<List<string>>();

                // Extract table data from DOCX
                using (var doc = WordprocessingDocument.Open(inputPath, false))
                {
                    var body = doc.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        // First, extract tables
                        foreach (var tbl in body.Elements<Table>())
                        {
                            foreach (var tr in tbl.Elements<TableRow>())
                            {
                                var row = tr.Elements<TableCell>()
                                    .Select(tc => string.Concat(tc.Descendants<Text>().Select(t => t.Text)).Trim())
                                    .ToList();
                                if (row.Any(c => !string.IsNullOrWhiteSpace(c)))
                                    rawRows.Add(row);
                            }
                        }

                        // If no tables found, extract paragraphs as single-column data
                        if (rawRows.Count == 0)
                        {
                            rawRows.Add(new List<string> { "Content" }); // Header
                            
                            foreach (var paragraph in body.Elements<Paragraph>())
                            {
                                var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text)).Trim();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    rawRows.Add(new List<string> { text });
                                }
                            }
                        }
                    }
                }

                if (rawRows.Count == 0)
                {
                    // Create a basic structure if no content found
                    rawRows.Add(new List<string> { "Content" });
                    rawRows.Add(new List<string> { "No content found in document" });
                }

                // Process the data
                var header = rawRows[0];
                var processed = new List<List<string>>();
                List<string>? lastRow = null;

                for (int i = 1; i < rawRows.Count; i++)
                {
                    var row = new List<string>(rawRows[i]);
                    while (row.Count < header.Count)
                        row.Add(string.Empty);

                    if (string.IsNullOrWhiteSpace(row[0]))
                    {
                        // Merge with previous row if first column is empty
                        if (lastRow != null)
                        {
                            for (int j = 1; j < row.Count; j++)
                            {
                                if (!string.IsNullOrWhiteSpace(row[j]))
                                    lastRow[j] = string.Join(" ", lastRow[j], row[j]).Trim();
                            }
                        }
                    }
                    else
                    {
                        lastRow = new List<string>(row);
                        processed.Add(lastRow);
                    }
                }

                // Create Excel file
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Sheet1");
                    
                    // Write headers
                    for (int c = 0; c < header.Count; c++)
                        ws.Cell(1, c + 1).Value = header[c];
                    
                    // Write data
                    for (int r = 0; r < processed.Count; r++)
                        for (int c = 0; c < processed[r].Count; c++)
                            ws.Cell(r + 2, c + 1).Value = processed[r][c];
                    
                    wb.SaveAs(outputPath);
                }

                _logger.LogInformation($"Successfully converted DOCX to Excel: {outputPath}");
            });
        }
    }
}
