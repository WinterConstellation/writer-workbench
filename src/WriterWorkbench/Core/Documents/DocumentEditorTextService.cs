namespace WriterWorkbench.Core.Documents;

public static class DocumentEditorTextService
{
    public static DocumentEditorTextView CreateView(WriterDocument document)
    {
        return new DocumentEditorTextView(
            ToPlainText(document.Paragraphs),
            document.Paragraphs.Count);
    }

    public static WriterDocument UpdateFromEditorText(
        WriterDocument existing,
        string title,
        string editorText,
        DocumentEditorTextView editorView)
    {
        return DocumentTextMergeService.UpdateFromPlainText(existing, title, editorText);
    }

    public static int CountEditorParagraphs(string editorText)
    {
        return editorText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(part => part.Trim())
            .Count(part => part.Length > 0);
    }

    private static string ToPlainText(IEnumerable<WriterParagraph> paragraphs)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            paragraphs.Select(paragraph => paragraph.Text.TrimEnd()));
    }
}
