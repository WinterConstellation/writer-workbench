using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class PreviewTextServiceTests
{
    [Fact]
    public void KeepsShortPreviewTextUnchanged()
    {
        var preview = PreviewTextService.CreatePreview("Short text", 100);

        Assert.Equal("Short text", preview);
    }

    [Fact]
    public void TruncatesLargePreviewText()
    {
        var text = new string('A', 10_000);

        var preview = PreviewTextService.CreatePreview(text, 1_000);

        Assert.True(preview.Length < text.Length);
        Assert.StartsWith(new string('A', 1_000), preview);
        Assert.Contains("미리보기", preview);
    }

    [Fact]
    public void CreatesBoundedPreviewForLargeDocument()
    {
        var document = LargeDocumentFactory.Create("stress-preview", "Stress", 15_000);
        var text = TextExportService.ToPlainText(document);

        var preview = PreviewTextService.CreatePreview(text);

        Assert.True(preview.Length <= PreviewTextService.DefaultMaxCharacters + 200);
        Assert.Contains("미리보기", preview);
    }
}
