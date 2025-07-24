namespace ChecklistGenerator.Models
{
    public class SurveyJSForm
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SurveyJSElement> Elements { get; set; } = new List<SurveyJSElement>();
        public List<SurveyJSPage> Pages { get; set; } = new List<SurveyJSPage>();
        public string ShowProgressBar { get; set; } = "top";
        public string CompleteText { get; set; } = "Complete";
        public string ShowQuestionNumbers { get; set; } = "on";
        public string QuestionTitleLocation { get; set; } = "top";
        public bool ShowNavigationButtons { get; set; } = true;
        public bool GoNextPageAutomatic { get; set; } = false;
        public bool ShowCompletedPage { get; set; } = true;
    }

    public class SurveyJSPage
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SurveyJSElement> Elements { get; set; } = new List<SurveyJSElement>();
    }

    public class SurveyJSElement
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
        public List<SurveyJSChoice>? Choices { get; set; }
        public string InputType { get; set; } = string.Empty;
        public int MaxLength { get; set; } = 0;
        public string Placeholder { get; set; } = string.Empty;
    }

    public class SurveyJSChoice
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
