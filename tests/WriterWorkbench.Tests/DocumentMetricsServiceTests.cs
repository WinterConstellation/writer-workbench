using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentMetricsServiceTests
{
    [Fact]
    public void CountsParagraphsCharactersAndPlainTextBytes()
    {
        var document = new WriterDocument(
            "scene-0001",
            "Metrics",
            [
                new WriterParagraph("p-1", "첫 문단", "body", [], []),
                new WriterParagraph("p-2", "Second", "body", [], [])
            ]);

        var metrics = DocumentMetricsService.Measure(document);

        Assert.Equal(2, metrics.ParagraphCount);
        Assert.Equal(10, metrics.CharacterCount);
        Assert.True(metrics.PlainTextUtf8Bytes >= 13);
    }

    [Fact]
    public void MeasuresPlainTextWithEditorParagraphRules()
    {
        var metrics = DocumentMetricsService.MeasurePlainText("  Alpha  \n\nA B\n\n\n");

        Assert.Equal(2, metrics.ParagraphCount);
        Assert.Equal(8, metrics.CharacterCount);
    }
}
