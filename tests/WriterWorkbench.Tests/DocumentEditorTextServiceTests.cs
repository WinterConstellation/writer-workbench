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
            [new WriterParagraph("p1", "Short text", "Body", [], [])]);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.Equal(1, view.ParagraphCount);
        Assert.Equal("Short text", view.Text);
    }

    [Fact]
    public void OpensLargeDocumentAsFullEditableText()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.Equal(document.Paragraphs.Count, view.ParagraphCount);
        Assert.Contains(document.Paragraphs[0].Text, view.Text);
        Assert.Contains(document.Paragraphs[^1].Text, view.Text);
    }

    [Fact]
    public void DoesNotSplitLargeDocument()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.Equal(document.Paragraphs.Count, view.ParagraphCount);
        Assert.Contains(document.Paragraphs[0].Text, view.Text);
        Assert.Contains(document.Paragraphs[^1].Text, view.Text);
    }

    [Fact]
    public void KeepsFullLargeDocumentInEditorEvenWhenLong()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);

        var view = DocumentEditorTextService.CreateView(document);

        Assert.True(view.Text.Length > 5_000);
        Assert.Contains(document.Paragraphs[0].Text, view.Text);
        Assert.Contains(document.Paragraphs[^1].Text, view.Text);
    }

    [Fact]
    public void UpdatesWholeLargeDocumentFromEditorText()
    {
        var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);
        var view = DocumentEditorTextService.CreateView(document);
        var editedText = view.Text.Replace(
            document.Paragraphs[0].Text,
            "Edited front paragraph",
            StringComparison.Ordinal);

        var updated = DocumentEditorTextService.UpdateFromEditorText(document, document.Title, editedText, view);

        Assert.Equal(document.Paragraphs.Count, updated.Paragraphs.Count);
        Assert.Equal("Edited front paragraph", updated.Paragraphs[0].Text);
        Assert.Equal(document.Paragraphs[^1].Text, updated.Paragraphs[^1].Text);
    }
}
