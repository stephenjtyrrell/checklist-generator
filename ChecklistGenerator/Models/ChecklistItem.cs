namespace ChecklistGenerator.Models
{
    public class ChecklistItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public ChecklistItemType Type { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public bool IsRequired { get; set; } = false;
        public string Description { get; set; } = string.Empty;
    }

    public enum ChecklistItemType
    {
        Text,
        Boolean,
        RadioGroup,
        Checkbox,
        Dropdown,
        Comment
    }
}
