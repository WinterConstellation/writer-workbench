using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class TextReplacementStoreTests
{
    [Fact]
    public async Task AddOrUpdateRoundtripsKoreanAndEllipsisReplacementAsUtf8()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new TextReplacementStore(paths);

        var created = await store.AddOrUpdateAsync("...", "…", CancellationToken.None);
        await store.AddOrUpdateAsync("안되", "안 돼", CancellationToken.None);

        var loaded = await store.LoadAsync(CancellationToken.None);
        var fileText = await File.ReadAllTextAsync(paths.TextReplacementsPath, CancellationToken.None);

        Assert.Equal("replace-0001", created.Id);
        Assert.Contains(loaded, rule => rule.Source == "..." && rule.Replacement == "…");
        Assert.Contains(loaded, rule => rule.Source == "안되" && rule.Replacement == "안 돼");
        Assert.Contains("안 돼", fileText);
        Assert.Contains("…", fileText);
    }

    [Fact]
    public void ApplyReplacesEnabledRulesWithoutChangingSourceRules()
    {
        var rules = new[]
        {
            new TextReplacementRule("replace-0001", "...", "…", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new TextReplacementRule("replace-0002", "안되", "안 돼", true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new TextReplacementRule("replace-0003", "초고", "완료", false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        var result = TextReplacementStore.Apply("문장... 이건 안되... 초고", rules);

        Assert.Equal("문장… 이건 안 돼… 초고", result.Text);
        Assert.Equal(3, result.ReplacementCount);
    }

    [Fact]
    public async Task DeleteRemovesRuleWithoutTouchingOtherRules()
    {
        var paths = ProjectPaths.ForRoot(NewRoot());
        var store = new TextReplacementStore(paths);
        var ellipsis = await store.AddOrUpdateAsync("...", "…", CancellationToken.None);
        await store.AddOrUpdateAsync("됬", "됐", CancellationToken.None);

        await store.DeleteAsync(ellipsis.Id, CancellationToken.None);

        var loaded = await store.LoadAsync(CancellationToken.None);
        Assert.DoesNotContain(loaded, rule => rule.Id == ellipsis.Id);
        Assert.Contains(loaded, rule => rule.Source == "됬" && rule.Replacement == "됐");
    }

    private static string NewRoot()
    {
        return Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
    }
}
