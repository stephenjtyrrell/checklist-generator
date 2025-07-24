using ChecklistGenerator.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.XSSF.UserModel;

namespace ChecklistGenerator.Tests.Helpers
{
    public static class TestDataHelper
    {
        public static List<ChecklistItem> CreateSampleChecklistItems(int count = 3)
        {
            var items = new List<ChecklistItem>();
            
            for (int i = 1; i <= count; i++)
            {
                items.Add(new ChecklistItem
                {
                    Id = $"test_item_{i}",
                    Text = $"Test question {i}",
                    Type = (ChecklistItemType)(i % 6), // Cycle through all types
                    IsRequired = i % 2 == 0,
                    Description = $"Description for test item {i}",
                    Options = i % 3 == 0 ? new List<string> { "Option A", "Option B", "Option C" } : new List<string>()
                });
            }
            
            return items;
        }

        public static MemoryStream CreateTestDocxWithTable(string[,] tableData)
        {
            var stream = new MemoryStream();
            
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var table = new Table();
                
                var tblPr = new TableProperties();
                var tblW = new TableWidth() { Width = "0", Type = TableWidthUnitValues.Auto };
                tblPr.Append(tblW);
                table.AppendChild(tblPr);

                int rows = tableData.GetLength(0);
                int cols = tableData.GetLength(1);

                for (int r = 0; r < rows; r++)
                {
                    var row = new TableRow();
                    for (int c = 0; c < cols; c++)
                    {
                        row.Append(CreateTableCell(tableData[r, c]));
                    }
                    table.Append(row);
                }

                body.Append(table);
            }

            stream.Position = 0;
            return stream;
        }

        public static MemoryStream CreateTestDocxWithParagraphs(params string[] paragraphs)
        {
            var stream = new MemoryStream();
            
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                foreach (var paragraphText in paragraphs)
                {
                    var para = new Paragraph();
                    var run = new Run();
                    run.Append(new Text(paragraphText));
                    para.Append(run);
                    body.Append(para);
                }
            }

            stream.Position = 0;
            return stream;
        }

        public static MemoryStream CreateTestExcel(string[] headers, string[,] data)
        {
            var stream = new MemoryStream();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            // Create header row
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Length; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }

            // Create data rows
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            
            for (int r = 0; r < rows; r++)
            {
                var row = sheet.CreateRow(r + 1);
                for (int c = 0; c < cols; c++)
                {
                    row.CreateCell(c).SetCellValue(data[r, c]);
                }
            }

            workbook.Write(stream);
            workbook.Close();
            
            stream.Position = 0;
            return stream;
        }

        public static MemoryStream CreateSimpleTestExcel(params string[] questions)
        {
            var headers = new[] { "Question" };
            var data = new string[questions.Length, 1];
            
            for (int i = 0; i < questions.Length; i++)
            {
                data[i, 0] = questions[i];
            }
            
            return CreateTestExcel(headers, data);
        }

        private static TableCell CreateTableCell(string text)
        {
            var cell = new TableCell();
            var paragraph = new Paragraph();
            var run = new Run();
            run.Append(new Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);
            return cell;
        }

        public static SurveyJSForm CreateSampleSurveyForm(int pageCount = 1, int questionsPerPage = 5)
        {
            var form = new SurveyJSForm
            {
                Title = "Test Survey",
                Description = "Test Description"
            };

            if (pageCount == 1)
            {
                for (int i = 1; i <= questionsPerPage; i++)
                {
                    form.Elements.Add(new SurveyJSElement
                    {
                        Name = $"question_{i}",
                        Title = $"Question {i}",
                        Type = "boolean",
                        IsRequired = i % 2 == 0
                    });
                }
            }
            else
            {
                for (int page = 1; page <= pageCount; page++)
                {
                    var surveyPage = new SurveyJSPage
                    {
                        Name = $"page_{page}",
                        Title = $"Section {page}"
                    };

                    for (int q = 1; q <= questionsPerPage; q++)
                    {
                        int questionNumber = (page - 1) * questionsPerPage + q;
                        surveyPage.Elements.Add(new SurveyJSElement
                        {
                            Name = $"question_{questionNumber}",
                            Title = $"Question {questionNumber}",
                            Type = "boolean",
                            IsRequired = questionNumber % 2 == 0
                        });
                    }

                    form.Pages.Add(surveyPage);
                }
            }

            return form;
        }

        public static Dictionary<string, object> CreateSampleSurveyData()
        {
            return new Dictionary<string, object>
            {
                { "question_1", "Sample answer" },
                { "question_2", true },
                { "question_3", "Option A" },
                { "question_4", false },
                { "question_5", 42 }
            };
        }
    }
}
