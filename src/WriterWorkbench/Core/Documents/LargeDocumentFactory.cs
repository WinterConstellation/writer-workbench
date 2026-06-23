namespace WriterWorkbench.Core.Documents;

public static class LargeDocumentFactory
{
    public static WriterDocument Create(
        string id,
        string title,
        int paragraphCount,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (paragraphCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(paragraphCount), "Paragraph count must be positive.");
        }

        var paragraphs = new List<WriterParagraph>(paragraphCount);
        for (var index = 1; index <= paragraphCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            paragraphs.Add(new WriterParagraph(
                $"p-{index:00000}",
                $"Large manuscript paragraph {index:00000}. This sentence verifies load, search, preview, and response time for stress testing.",
                "body",
                [],
                []));

            if (index == paragraphCount || index % 250 == 0)
            {
                progress?.Report(index);
            }
        }

        return new WriterDocument(id, title, paragraphs);
    }
}
