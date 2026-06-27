namespace WriterWorkbench.Core.Documents;

public sealed record DocumentEditorTextView(
    string Text,
    int ParagraphCount)
{
    public static DocumentEditorTextView Empty { get; } = new("", 0);
}
