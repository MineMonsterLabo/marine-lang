namespace MarineLang.CodeAnalysis
{
    public class CompletionItem
    {
        public string Title { get; }
        public string Description { get; }

        public string Content { get; }

        public int Order { get; }

        public CompletionFilterFlags Flags { get; }

        public CompletionItem(string title, string description, string content, int order, CompletionFilterFlags flags)
        {
            Title = title;
            Description = description;
            Content = content;

            Order = order;

            Flags = flags;
        }
    }
}