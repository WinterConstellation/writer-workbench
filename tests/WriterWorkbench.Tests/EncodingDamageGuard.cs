namespace WriterWorkbench.Tests;

internal static class EncodingDamageGuard
{
    private static readonly string[] Markers =
    [
        "\u73E5\uB347\uD02C",
        "\u8E42\uB4ED\uC35D",
        "\u6028\uB4EC\uAC1A",
        "\uBA84\uCB5B",
        "\uF9CF\uB085\uC84A",
        "\uFFFD"
    ];

    private static readonly string[] SourceExtensions =
    [
        ".cs",
        ".xaml",
        ".js",
        ".html",
        ".css",
        ".md",
        ".txt"
    ];

    private static readonly string[] SourceDirectories =
    [
        "src",
        "tests",
        "docs"
    ];

    public static IReadOnlyList<EncodingDamageFinding> FindDamage(string text)
    {
        return Markers
            .Where(marker => text.Contains(marker, StringComparison.Ordinal))
            .Select(marker => new EncodingDamageFinding("", 0, marker, text))
            .ToList();
    }

    public static IReadOnlyList<EncodingDamageFinding> FindDamageInRepository(string repositoryRoot)
    {
        var findings = new List<EncodingDamageFinding>();
        foreach (var directoryName in SourceDirectories)
        {
            var directory = Path.Combine(repositoryRoot, directoryName);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (!SourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(repositoryRoot, file);
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;
                    foreach (var marker in Markers)
                    {
                        if (line.Contains(marker, StringComparison.Ordinal))
                        {
                            findings.Add(new EncodingDamageFinding(relativePath, lineNumber, marker, line.Trim()));
                        }
                    }
                }
            }
        }

        return findings;
    }
}

internal sealed record EncodingDamageFinding(
    string Path,
    int LineNumber,
    string Marker,
    string Line);
