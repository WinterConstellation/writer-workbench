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
}
