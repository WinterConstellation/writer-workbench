using System.Text.Json;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentSerializationTests
{
    [Fact]
    public void SerializesAndDeserializesDocument()
    {
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [new WriterParagraph("p-1", "본문", "body", ["draft"], ["memo"])]);

        var json = JsonSerializer.Serialize(document);
        var loaded = JsonSerializer.Deserialize<WriterDocument>(json);

        Assert.NotNull(loaded);
        Assert.Equal("scene-0001", loaded.Id);
        Assert.Equal("본문", loaded.Paragraphs[0].Text);
    }
}
