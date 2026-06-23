using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class StarterDocumentRepairServiceTests
{
    [Fact]
    public void AddsDefaultParagraphOnlyWhenDocumentHasNoParagraphs()
    {
        var empty = new WriterDocument("scene-0001", "첫 장면", []);

        var repaired = StarterDocumentRepairService.RepairIfEmpty(empty);

        Assert.Single(repaired.Paragraphs);
        Assert.Equal("여기에 원고를 씁니다.", repaired.Paragraphs[0].Text);
    }

    [Fact]
    public void LeavesExistingTextUntouched()
    {
        var document = new WriterDocument(
            "scene-0001",
            "첫 장면",
            [new WriterParagraph("p-1", "이미 쓴 문장", "body", [], [])]);

        var repaired = StarterDocumentRepairService.RepairIfEmpty(document);

        Assert.Same(document, repaired);
    }
}
