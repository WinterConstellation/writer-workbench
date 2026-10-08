using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class EditorInputPerformancePolicyTests
{
    [Theory]
    [InlineData(100, PreviewUpdateMode.Immediate)]
    [InlineData(EditorInputPerformancePolicy.ImmediatePreviewCharacterLimit, PreviewUpdateMode.Immediate)]
    [InlineData(EditorInputPerformancePolicy.ImmediatePreviewCharacterLimit + 1, PreviewUpdateMode.Debounced)]
    [InlineData(EditorInputPerformancePolicy.LivePreviewCharacterLimit, PreviewUpdateMode.Debounced)]
    [InlineData(EditorInputPerformancePolicy.LivePreviewCharacterLimit + 1, PreviewUpdateMode.Paused)]
    public void ChoosesPreviewUpdateModeFromTextLength(int characterCount, PreviewUpdateMode expected)
    {
        Assert.Equal(expected, EditorInputPerformancePolicy.GetPreviewUpdateMode(characterCount));
    }

    [Fact]
    public void CreatesPausedPreviewMessage()
    {
        var message = EditorInputPerformancePolicy.CreatePausedPreviewMessage(80_000);

        Assert.Contains("미리보기 갱신 중지", message);
        Assert.Contains("80,000", message);
    }
}
