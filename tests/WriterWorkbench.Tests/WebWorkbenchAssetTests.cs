namespace WriterWorkbench.Tests;

public sealed class WebWorkbenchAssetTests
{
    [Fact]
    public void WebWorkbenchAssetsAreCopiedNextToApplication()
    {
        var appDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!;
        var assetDirectory = Path.Combine(appDirectory, "WebWorkbench");

        Assert.True(File.Exists(Path.Combine(assetDirectory, "index.html")));
        Assert.True(File.Exists(Path.Combine(assetDirectory, "styles.css")));
        Assert.True(File.Exists(Path.Combine(assetDirectory, "app.js")));
    }
}
