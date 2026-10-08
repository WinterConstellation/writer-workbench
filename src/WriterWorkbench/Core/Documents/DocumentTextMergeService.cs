namespace WriterWorkbench.Core.Documents;

public static class DocumentTextMergeService
{
    public static WriterDocument UpdateFromPlainText(
        WriterDocument existing,
        string title,
        string plainText)
    {
        var texts = plainText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

        var paragraphs = new List<WriterParagraph>(texts.Count);
        for (var index = 0; index < texts.Count; index++)
        {
            if (index < existing.Paragraphs.Count)
            {
                paragraphs.Add(existing.Paragraphs[index] with { Text = texts[index] });
                continue;
            }

            paragraphs.Add(new WriterParagraph(
                $"p-{index + 1:0000}",
                texts[index],
                "body",
                [],
                []));
        }

        return existing with
        {
            Title = string.IsNullOrWhiteSpace(title) ? existing.Id : title.Trim(),
            Paragraphs = paragraphs
        };
    }
}
