using System.Globalization;
using System.IO;

namespace WriterWorkbench.Core.WebWorkbench;

public static class WebWorkbenchAssetVersion
{
    public static Uri CreateIndexUri(string indexPath)
    {
        var version = CreateVersion(indexPath);
        return new UriBuilder(new Uri(indexPath))
        {
            Query = $"wwv={Uri.EscapeDataString(version)}"
        }.Uri;
    }

    private static string CreateVersion(string indexPath)
    {
        var assetDirectory = Path.GetDirectoryName(indexPath) ?? "";
        var assets = new[]
        {
            indexPath,
            Path.Combine(assetDirectory, "app.js"),
            Path.Combine(assetDirectory, "styles.css")
        };

        return assets
            .Where(File.Exists)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max()
            .Ticks
            .ToString("D", CultureInfo.InvariantCulture);
    }
}
