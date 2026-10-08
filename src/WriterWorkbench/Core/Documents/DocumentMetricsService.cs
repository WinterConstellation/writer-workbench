using System.Text;

namespace WriterWorkbench.Core.Documents;

public static class DocumentMetricsService
{
    public static DocumentMetrics Measure(WriterDocument document)
    {
        var plainText = TextExportService.ToPlainText(document);
        return new DocumentMetrics(
            document.Paragraphs.Count,
            document.Paragraphs.Sum(paragraph => paragraph.Text.Length),
            Encoding.UTF8.GetByteCount(plainText));
    }

    public static DocumentMetrics MeasurePlainText(string plainText)
    {
        var paragraphs = plainText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();
        var normalizedText = string.Join(Environment.NewLine + Environment.NewLine, paragraphs);

        return new DocumentMetrics(
            paragraphs.Count,
            paragraphs.Sum(paragraph => paragraph.Length),
            Encoding.UTF8.GetByteCount(normalizedText));
    }
}
