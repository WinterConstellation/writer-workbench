namespace WriterWorkbench.Tests;

public sealed class EncodingDamageGuardTests
{
    [Fact]
    public void DetectsKnownMojibakeMarkers()
    {
        var report = EncodingDamageGuard.FindDamage("status = \"\u73E5\uB347\uD02C\";");

        Assert.Contains(report, item => item.Marker == "\u73E5\uB347\uD02C");
    }

    [Fact]
    public void SourceFilesDoNotContainKnownMojibakeMarkers()
    {
        var repositoryRoot = FindRepositoryRoot();

        var findings = EncodingDamageGuard.FindDamageInRepository(repositoryRoot);

        Assert.Empty(findings);
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "WriterWorkbench.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? "";
        }

        throw new DirectoryNotFoundException("Could not locate WriterWorkbench.sln.");
    }
}
