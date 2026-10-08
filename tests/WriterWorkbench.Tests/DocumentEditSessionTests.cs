using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentEditSessionTests
{
    [Fact]
    public void CreatesSnapshotFromEditedTitleAndPlainText()
    {
        var original = new WriterDocument(
            "scene-100",
            "Before",
            [
                new WriterParagraph("p-1", "First paragraph", "Body", ["draft"], ["keep"]),
                new WriterParagraph("p-2", "Second paragraph", "Body", [], [])
            ]);
        var session = new DocumentEditSession(original);

        session.Title = "After";
        session.PlainText = "Changed first paragraph\r\n\r\nChanged second paragraph";

        var snapshot = session.CreateSnapshot();

        Assert.Equal("scene-100", snapshot.Id);
        Assert.Equal("After", snapshot.Title);
        Assert.Collection(
            snapshot.Paragraphs,
            paragraph =>
            {
                Assert.Equal("p-1", paragraph.Id);
                Assert.Equal("Changed first paragraph", paragraph.Text);
                Assert.Equal(["draft"], paragraph.Tags);
                Assert.Equal(["keep"], paragraph.Notes);
            },
            paragraph =>
            {
                Assert.Equal("p-2", paragraph.Id);
                Assert.Equal("Changed second paragraph", paragraph.Text);
            });
    }

    [Fact]
    public void MeasuresEditedPlainText()
    {
        var original = new WriterDocument(
            "scene-101",
            "Metrics",
            [new WriterParagraph("p-1", "Before", "Body", [], [])]);
        var session = new DocumentEditSession(original)
        {
            PlainText = "One\r\n\r\nTwo"
        };

        var metrics = session.Measure();

        Assert.Equal(2, metrics.ParagraphCount);
        Assert.Equal(6, metrics.CharacterCount);
    }
}
