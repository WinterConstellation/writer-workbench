using System.Globalization;
using System.Text;

namespace WriterWorkbench.Core.Documents;

public static class WordFrequencyService
{
    public static WordFrequencyResult Analyze(
        IEnumerable<WordFrequencyDocument> documents,
        int minimumCount = 2,
        int limit = 120)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tokenCount = 0;
        var documentCount = 0;

        foreach (var document in documents)
        {
            documentCount++;
            foreach (var rawToken in SplitWhitespaceTokens(document.Text))
            {
                var token = NormalizeToken(rawToken);
                if (token.Length == 0)
                {
                    continue;
                }

                tokenCount++;
                counts[token] = counts.GetValueOrDefault(token) + 1;
            }
        }

        var entries = counts
            .Where(entry => entry.Value >= Math.Max(1, minimumCount))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.CurrentCulture)
            .Take(Math.Max(1, limit))
            .Select(entry => new WordFrequencyEntry(entry.Key, entry.Value))
            .ToList();

        return new WordFrequencyResult(documentCount, tokenCount, counts.Count, entries);
    }

    private static IEnumerable<string> SplitWhitespaceTokens(string text)
    {
        var normalized = (text ?? "").Replace("\r\n", "\n", StringComparison.Ordinal);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                if (builder.Length > 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                }

                continue;
            }

            builder.Append(character);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static string NormalizeToken(string token)
    {
        var normalized = (token ?? "").Normalize(NormalizationForm.FormC);
        var start = 0;
        var end = normalized.Length - 1;

        while (start <= end && IsOuterMark(normalized[start]))
        {
            start++;
        }

        while (end >= start && IsOuterMark(normalized[end]))
        {
            end--;
        }

        return start <= end
            ? normalized[start..(end + 1)]
            : "";
    }

    private static bool IsOuterMark(char character)
    {
        if (char.IsWhiteSpace(character))
        {
            return true;
        }

        var category = char.GetUnicodeCategory(character);
        return category is
            UnicodeCategory.OpenPunctuation or
            UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or
            UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation or
            UnicodeCategory.DashPunctuation or
            UnicodeCategory.MathSymbol or
            UnicodeCategory.CurrencySymbol or
            UnicodeCategory.ModifierSymbol or
            UnicodeCategory.OtherSymbol;
    }
}

public sealed record WordFrequencyDocument(string DocumentId, string Title, string Text);

public sealed record WordFrequencyResult(
    int DocumentCount,
    int TokenCount,
    int UniqueWordCount,
    IReadOnlyList<WordFrequencyEntry> Entries);

public sealed record WordFrequencyEntry(string Word, int Count);
