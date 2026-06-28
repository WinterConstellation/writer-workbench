namespace WriterWorkbench.Core.Documents;

public static class LargeDocumentFactory
{
    private const string StressSentence =
        "Large manuscript paragraph {0:00000}. This sentence verifies load, search, preview, and response time for stress testing.";

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
                FormatStressSentence(index),
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

    public static WriterDocument CreateByCharacterCount(
        string id,
        string title,
        int targetCharacterCount,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (targetCharacterCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCharacterCount), "Target character count must be positive.");
        }

        var paragraphs = new List<WriterParagraph>();
        var generatedCharacters = 0;
        var index = 1;
        while (generatedCharacters < targetCharacterCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remaining = targetCharacterCount - generatedCharacters;
            var text = FormatStressSentence(index);
            if (text.Length > remaining)
            {
                text = text[..remaining].TrimEnd();
                if (text.Length < remaining)
                {
                    text += new string('.', remaining - text.Length);
                }
            }

            if (text.Length == 0)
            {
                break;
            }

            paragraphs.Add(new WriterParagraph(
                $"p-{index:00000}",
                text,
                "body",
                [],
                []));
            generatedCharacters += text.Length;
            index++;

            if (generatedCharacters == targetCharacterCount || generatedCharacters % 250 == 0)
            {
                progress?.Report(generatedCharacters);
            }
        }

        progress?.Report(generatedCharacters);
        return new WriterDocument(id, title, paragraphs);
    }

    private static string FormatStressSentence(int index)
    {
        return string.Format(StressSentence, index);
    }
}
