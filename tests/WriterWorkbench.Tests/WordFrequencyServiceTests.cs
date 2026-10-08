using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class WordFrequencyServiceTests
{
    [Fact]
    public void AnalyzeCountsWhitespaceTokensAndTrimsOuterPunctuation()
    {
        var result = WordFrequencyService.Analyze(
            [
                new WordFrequencyDocument(
                    "scene-0001",
                    "첫 장면",
                    "검은 고양이가 왔다. 검은 고양이는 봤다, 검은.")
            ]);

        Assert.Equal(7, result.TokenCount);
        Assert.Equal(5, result.UniqueWordCount);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("검은", entry.Word);
        Assert.Equal(3, entry.Count);
    }

    [Fact]
    public void AnalyzeAggregatesSelectedDocuments()
    {
        var result = WordFrequencyService.Analyze(
            [
                new WordFrequencyDocument("scene-0001", "첫 장면", "문장 하나 반복"),
                new WordFrequencyDocument("scene-0002", "둘째 장면", "반복 문장 둘")
            ]);

        Assert.Equal(2, result.DocumentCount);
        Assert.Equal(6, result.TokenCount);
        Assert.Contains(result.Entries, entry => entry.Word == "문장" && entry.Count == 2);
        Assert.Contains(result.Entries, entry => entry.Word == "반복" && entry.Count == 2);
    }

    [Fact]
    public void AnalyzeKeepsOnlyRepeatedWordsByDefault()
    {
        var result = WordFrequencyService.Analyze(
            [
                new WordFrequencyDocument("scene-0001", "첫 장면", "하나 둘 셋")
            ]);

        Assert.Empty(result.Entries);
    }
}
