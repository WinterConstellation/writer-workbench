namespace WriterWorkbench.Core.Documents;

public static class DocumentEditorTextService
{
    public const int DefaultLargeDocumentEditorLimit = 5_000;

    public static DocumentEditorTextView CreateView(
        WriterDocument document,
        int maxEditableCharacters = DefaultLargeDocumentEditorLimit)
    {
        if (maxEditableCharacters < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEditableCharacters), "Editor limit must be positive.");
        }

        var visibleParagraphs = TakeVisibleParagraphs(document, maxEditableCharacters);
        var visibleText = ToPlainText(visibleParagraphs);
        var isSegmentMode = visibleParagraphs.Count < document.Paragraphs.Count;
        var remainderText = isSegmentMode
            ? ToPlainText(document.Paragraphs.Skip(visibleParagraphs.Count))
            : "";

        return new DocumentEditorTextView(
            visibleText,
            isSegmentMode,
            visibleParagraphs.Count,
            remainderText,
            document.Paragraphs.Count);
    }

    public static WriterDocument UpdateFromEditorText(
        WriterDocument existing,
        string title,
        string editorText,
        DocumentEditorTextView editorView)
    {
        if (!editorView.IsSegmentMode)
        {
            return DocumentTextMergeService.UpdateFromPlainText(existing, title, editorText);
        }

        var visibleCount = Math.Clamp(editorView.VisibleParagraphCount, 0, existing.Paragraphs.Count);
        var visibleDocument = existing with
        {
            Paragraphs = existing.Paragraphs.Take(visibleCount).ToList()
        };
        var updatedVisibleDocument = DocumentTextMergeService.UpdateFromPlainText(visibleDocument, title, editorText);
        var hiddenParagraphs = existing.Paragraphs.Skip(visibleCount);
        var mergedParagraphs = updatedVisibleDocument.Paragraphs
            .Concat(hiddenParagraphs)
            .ToList();

        return existing with
        {
            Title = updatedVisibleDocument.Title,
            Paragraphs = mergedParagraphs
        };
    }

    public static int CountEditorParagraphs(string editorText)
    {
        return editorText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(part => part.Trim())
            .Count(part => part.Length > 0);
    }

    private static IReadOnlyList<WriterParagraph> TakeVisibleParagraphs(
        WriterDocument document,
        int maxEditableCharacters)
    {
        var paragraphs = new List<WriterParagraph>();
        var currentLength = 0;
        var separatorLength = (Environment.NewLine + Environment.NewLine).Length;

        foreach (var paragraph in document.Paragraphs)
        {
            var paragraphLength = paragraph.Text.TrimEnd().Length;
            var nextLength = currentLength
                             + (paragraphs.Count == 0 ? 0 : separatorLength)
                             + paragraphLength;

            if (paragraphs.Count > 0 && nextLength > maxEditableCharacters)
            {
                break;
            }

            paragraphs.Add(paragraph);
            currentLength = nextLength;

            if (currentLength >= maxEditableCharacters)
            {
                break;
            }
        }

        return paragraphs;
    }

    private static string ToPlainText(IEnumerable<WriterParagraph> paragraphs)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            paragraphs.Select(paragraph => paragraph.Text.TrimEnd()));
    }
}
