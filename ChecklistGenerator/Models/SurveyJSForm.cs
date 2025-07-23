namespace ChecklistGenerator.Models
{
    public class SurveyJSForm
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SurveyJSElement> Elements { get; set; } = new List<SurveyJSElement>();
        public bool ShowProgressBar { get; set; } = false;
        public string CompleteText { get; set; } = "Complete";
    }

    public class SurveyJSElement
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
        public List<SurveyJSChoice>? Choices { get; set; }
    }

    public class SurveyJSChoice
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
