using NPOI.XWPF.UserModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ChecklistGenerator.Services
{
    public class DocumentConverterService
    {
        public async Task<Stream> ConvertDocToDocxAsync(Stream docStream, string originalFileName)
        {
            try
            {
                // Reset stream position
                docStream.Position = 0;

                // Extract text and create new DOCX (simplified approach)
                var convertedStream = await ConvertByTextExtraction(docStream, originalFileName);
                
                return convertedStream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert .doc to .docx: {ex.Message}", ex);
            }
        }

        private async Task<Stream> ConvertByTextExtraction(Stream docStream, string originalFileName)
        {
            // Extract text from the .doc file
            var extractedText = await ExtractTextFromDocFile(docStream);
            
            // Create a new DOCX file with the extracted text
            var memoryStream = new MemoryStream();
            
            using (var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
            {
                // Add main document part
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Split text into paragraphs and add to document
                var lines = extractedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(cleanLine))
                        continue;

                    // Create paragraph
                    var paragraph = new Paragraph();
                    var run = new Run();
                    var text = new Text(cleanLine);
                    
                    run.Append(text);
                    paragraph.Append(run);
                    body.Append(paragraph);
                }

                // If no content was extracted, add a note
                if (!lines.Any(l => !string.IsNullOrWhiteSpace(l.Trim())))
                {
                    var paragraph = new Paragraph();
                    var run = new Run();
                    var text = new Text("No readable content could be extracted from the original .doc file. " +
                                      "The file may be corrupted, encrypted, or contain complex formatting.");
                    
                    run.Append(text);
                    paragraph.Append(run);
                    body.Append(paragraph);
                }

                document.Save();
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<string> ExtractTextFromDocFile(Stream docStream)
        {
            try
            {
                docStream.Position = 0;
                using var memoryStream = new MemoryStream();
                await docStream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                
                // Extract readable text from binary data
                var textChunks = new List<string>();
                var currentChunk = new List<char>();
                
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    var b = bytes[i];
                    
                    // Look for readable ASCII characters
                    if (b >= 32 && b <= 126)
                    {
                        currentChunk.Add((char)b);
                    }
                    else if (currentChunk.Count > 3) // Only save chunks with at least 4 characters
                    {
                        var chunk = new string(currentChunk.ToArray()).Trim();
                        if (chunk.Length > 3 && !IsDocumentArtifact(chunk) && ContainsLetters(chunk))
                        {
                            textChunks.Add(chunk);
                        }
                        currentChunk.Clear();
                    }
                    else
                    {
                        currentChunk.Clear();
                    }
                }
                
                // Add any remaining chunk
                if (currentChunk.Count > 3)
                {
                    var chunk = new string(currentChunk.ToArray()).Trim();
                    if (chunk.Length > 3 && !IsDocumentArtifact(chunk) && ContainsLetters(chunk))
                    {
                        textChunks.Add(chunk);
                    }
                }
                
                // Join chunks with line breaks for better paragraph structure
                var extractedText = string.Join("\n", textChunks.Where(chunk => 
                    !string.IsNullOrWhiteSpace(chunk) && chunk.Length > 2));
                
                return extractedText;
            }
            catch (Exception)
            {
                return "Failed to extract text from .doc file.";
            }
        }

        private bool IsDocumentArtifact(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;
                
            var lowerText = text.ToLower();
            
            // Filter out common Word document artifacts
            return lowerText.Contains("microsoft") ||
                   lowerText.Contains("word") ||
                   lowerText.Contains("times") ||
                   lowerText.Contains("arial") ||
                   lowerText.Contains("calibri") ||
                   lowerText.Contains("font") ||
                   text.Length < 3 ||
                   System.Text.RegularExpressions.Regex.IsMatch(text, @"^[0-9\s\.\-]+$") ||
                   System.Text.RegularExpressions.Regex.IsMatch(text, @"^[^a-zA-Z]*$");
        }

        private bool ContainsLetters(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"[a-zA-Z]");
        }
    }
}
