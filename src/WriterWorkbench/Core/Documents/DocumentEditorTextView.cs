namespace WriterWorkbench.Core.Documents;

public sealed record DocumentEditorTextView(
    string Text,
    bool IsSegmentMode,
    int VisibleParagraphCount,
    string RemainderText,
    int TotalParagraphCount)
{
    public static DocumentEditorTextView Empty { get; } = new("", false, 0, "", 0);
}
