using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class TextExportServiceTests
{
    [Fact]
    public void ExportsParagraphsWithBlankLineBetweenThem()
    {
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [
                new WriterParagraph("p-1", "첫 문단입니다.", "body", [], []),
                new WriterParagraph("p-2", "Second paragraph.", "body", [], [])
            ]);

        var text = TextExportService.ToPlainText(document);

        Assert.Equal("첫 문단입니다.\r\n\r\nSecond paragraph.", text);
    }
}
