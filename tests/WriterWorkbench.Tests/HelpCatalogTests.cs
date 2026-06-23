using WriterWorkbench.Core.Help;

namespace WriterWorkbench.Tests;

public sealed class HelpCatalogTests
{
    [Fact]
    public void ContainsRolesForPrimaryWorkbenchItems()
    {
        var topics = HelpCatalog.All;

        Assert.Contains(topics, topic => topic.Item == "새 프로젝트" && topic.Role.Contains("프로젝트"));
        Assert.Contains(topics, topic => topic.Item == "편집기" && topic.Role.Contains("본문"));
        Assert.Contains(topics, topic => topic.Item == "미리보기" && topic.Role.Contains("확인"));
        Assert.Contains(topics, topic => topic.Item == "집중 40:00" && topic.Role.Contains("집필"));
        Assert.Contains(topics, topic => topic.Item == "프리셋 1/2/3" && topic.Role.Contains("창 배치"));
        Assert.Contains(topics, topic => topic.Item == "진행 그래프" && topic.Role.Contains("남은 시간"));
    }

    [Fact]
    public void EveryHelpTopicHasActionableText()
    {
        foreach (var topic in HelpCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(topic.Section));
            Assert.False(string.IsNullOrWhiteSpace(topic.Item));
            Assert.True(topic.Role.Length >= 8, $"{topic.Item} role is too short.");
            Assert.True(topic.Usage.Length >= 8, $"{topic.Item} usage is too short.");
        }
    }
}
