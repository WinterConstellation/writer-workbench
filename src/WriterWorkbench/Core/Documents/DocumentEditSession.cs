namespace WriterWorkbench.Core.Documents;

public sealed class DocumentEditSession
{
    private WriterDocument _document;

    public DocumentEditSession(WriterDocument document)
    {
        _document = document;
        Title = document.Title;
        PlainText = TextExportService.ToPlainText(document);
    }

    public string Title { get; set; }

    public string PlainText { get; set; }

    public WriterDocument CreateSnapshot()
    {
        return DocumentTextMergeService.UpdateFromPlainText(_document, Title, PlainText);
    }

    public DocumentMetrics Measure()
    {
        return DocumentMetricsService.Measure(CreateSnapshot());
    }

    public void AcceptSnapshot(WriterDocument document)
    {
        _document = document;
        Title = document.Title;
        PlainText = TextExportService.ToPlainText(document);
    }
}
