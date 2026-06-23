namespace WriterWorkbench.Core.Documents;

public static class TextExportService
{
    public static string ToPlainText(WriterDocument document)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            document.Paragraphs.Select(paragraph => paragraph.Text.TrimEnd()));
    }
}
