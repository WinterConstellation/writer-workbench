using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class LargeDocumentFactoryTests
{
    [Fact]
    public void CreatesDeterministicLargeDocument()
    {
        var document = LargeDocumentFactory.Create("scene-stress", "Stress", paragraphCount: 1_000);

        Assert.Equal("scene-stress", document.Id);
        Assert.Equal("Stress", document.Title);
        Assert.Equal(1_000, document.Paragraphs.Count);
        Assert.Equal("p-00001", document.Paragraphs[0].Id);
        Assert.Contains("01000", document.Paragraphs[^1].Text);
    }

    [Fact]
    public void ReportsGenerationProgress()
    {
        var progress = new CapturingProgress();

        var document = LargeDocumentFactory.Create("scene-progress", "Progress", 1_000, progress);

        Assert.Equal(1_000, document.Paragraphs.Count);
        Assert.Contains(1_000, progress.Samples);
        Assert.True(progress.Samples.Count >= 2);
    }

    [Fact]
    public void CreatesStressDocumentByCharacterCount()
    {
        var progress = new CapturingProgress();

        var document = LargeDocumentFactory.CreateByCharacterCount("scene-15k", "Stress 15k", 15_000, progress);
        var metrics = DocumentMetricsService.Measure(document);

        Assert.Equal("scene-15k", document.Id);
        Assert.Equal("Stress 15k", document.Title);
        Assert.Equal(15_000, metrics.CharacterCount);
        Assert.InRange(document.Paragraphs.Count, 1, 200);
        Assert.Contains(15_000, progress.Samples);
    }

    private sealed class CapturingProgress : IProgress<int>
    {
        public List<int> Samples { get; } = [];

        public void Report(int value)
        {
            Samples.Add(value);
        }
    }
}
