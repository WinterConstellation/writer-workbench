using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentTextMergeServiceTests
{
    [Fact]
    public void PreservesParagraphMetadataWhenTextChanges()
    {
        var existing = new WriterDocument(
            "scene-0001",
            "Old",
            [
                new WriterParagraph("p-a", "old one", "heading", ["keep"], ["note"]),
                new WriterParagraph("p-b", "old two", "body", ["tag"], [])
            ]);

        var updated = DocumentTextMergeService.UpdateFromPlainText(
            existing,
            "New",
            "new one\r\n\r\nnew two\r\n\r\nnew three");

        Assert.Equal("New", updated.Title);
        Assert.Equal("p-a", updated.Paragraphs[0].Id);
        Assert.Equal("heading", updated.Paragraphs[0].Style);
        Assert.Equal(["keep"], updated.Paragraphs[0].Tags);
        Assert.Equal(["note"], updated.Paragraphs[0].Notes);
        Assert.Equal("new one", updated.Paragraphs[0].Text);
        Assert.Equal("p-b", updated.Paragraphs[1].Id);
        Assert.Equal("new two", updated.Paragraphs[1].Text);
        Assert.Equal("p-0003", updated.Paragraphs[2].Id);
        Assert.Equal("body", updated.Paragraphs[2].Style);
    }
}
