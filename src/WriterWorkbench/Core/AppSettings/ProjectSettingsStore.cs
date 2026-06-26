using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WriterWorkbench.Core.AppSettings;

public sealed class ProjectSettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<ProjectAppSettings> LoadOrCreateAsync(CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            var created = ProjectAppSettings.Default;
            await SaveAsync(created, token);
            return created;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return Normalize(
                await JsonSerializer.DeserializeAsync<ProjectAppSettings>(stream, JsonOptions, token)
                ?? ProjectAppSettings.Default);
        }
        catch (JsonException)
        {
            return ProjectAppSettings.Default;
        }
        catch (IOException)
        {
            return ProjectAppSettings.Default;
        }
    }

    public async Task SaveAsync(ProjectAppSettings settings, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(
            tempPath,
            JsonSerializer.Serialize(Normalize(settings), JsonOptions),
            token);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private static ProjectAppSettings Normalize(ProjectAppSettings settings)
    {
        return settings with
        {
            Schema = NormalizeOrDefault(settings.Schema, "app"),
            LastWorkspaceLabel = NormalizeOrDefault(settings.LastWorkspaceLabel, "마지막 작업"),
            LastSurface = NormalizeOrDefault(settings.LastSurface, "html-workbench"),
            LastSceneId = string.IsNullOrWhiteSpace(settings.LastSceneId) ? null : settings.LastSceneId.Trim()
        };
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        var normalized = value?.Trim() ?? "";
        return normalized.Length == 0 ? fallback : normalized;
    }
}
