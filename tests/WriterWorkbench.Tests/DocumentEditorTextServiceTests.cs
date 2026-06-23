using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentEditorTextServiceTests
{
    [Fact]
    public void KeepsSmallDocumentEditable()
    {
        var document = new WriterDocument(
            "scene-small",
            "Small",
            [new WriterParagraph("p-1", "Short text", "body", [], [])]);

        var view = DocumentEditorTextService.CreateView(document, 1_000);

        Assert.False(view.IsSegmentMode);
        Assert.Equal(1, view.VisibleParagraphCount);
        Assert.Equal("Short text", view.Text);
    }

    [Fact]
    public void OpensLargeDocumentAsEditableBoundedSegment()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.True(view.IsSegmentMode);
        Assert.InRange(view.VisibleParagraphCount, 1, document.Paragraphs.Count - 1);
        Assert.True(view.Text.Length <= DocumentEditorTextService.DefaultLargeDocumentEditorLimit);
        Assert.Contains(document.Paragraphs[0].Text, view.Text);
        Assert.DoesNotContain("preview mode", view.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeepsDefaultLargeDocumentSegmentSmallEnoughForResponsiveTyping()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.True(view.IsSegmentMode);
        Assert.True(view.Text.Length <= 5_000);
    }

    [Fact]
    public void MergesEditedSegmentWithoutReplacingRemainingParagraphs()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);
        var view = DocumentEditorTextService.CreateView(document, 20_000);
        var hiddenParagraph = document.Paragraphs[view.VisibleParagraphCount];
        var editedText = view.Text.Replace(
            document.Paragraphs[0].Text,
            "Edited front paragraph",
            StringComparison.Ordinal);

        var updated = DocumentEditorTextService.UpdateFromEditorText(document, "Renamed", editedText, view);

        Assert.Equal("Renamed", updated.Title);
        Assert.Equal(document.Paragraphs.Count, updated.Paragraphs.Count);
        Assert.Equal("Edited front paragraph", updated.Paragraphs[0].Text);
        Assert.Equal(hiddenParagraph, updated.Paragraphs[view.VisibleParagraphCount]);
        Assert.Equal(document.Paragraphs[^1].Text, updated.Paragraphs[^1].Text);
    }
}
