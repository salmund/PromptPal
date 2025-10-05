namespace PromptPal
{
    public class Prompt
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? Category { get; set; }
        public string? Tags { get; set; }
    }
}
