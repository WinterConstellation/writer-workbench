using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Appearance;

namespace WriterWorkbench.Core.Application;

public sealed class AppSessionStateService(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WriterWorkbench",
        "app.session.json");

    public async Task<AppSessionState> LoadAsync(CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            return AppSessionState.Empty;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<AppSessionState>(stream, JsonOptions, token)
                   ?? AppSessionState.Empty;
        }
        catch (JsonException)
        {
            return AppSessionState.Empty;
        }
        catch (IOException)
        {
            return AppSessionState.Empty;
        }
    }

    public async Task SaveAsync(AppSessionState state, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(Normalize(state), JsonOptions);
        await File.WriteAllTextAsync(tempPath, json, token);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private static AppSessionState Normalize(AppSessionState state)
    {
        var surface = state.Surface is AppSessionState.EditorSurface
            or AppSessionState.PreviewSurface
            or AppSessionState.MainSurface
            ? state.Surface
            : AppSessionState.EditorSurface;

        var presetSlot = state.PresetSlot is >= 1 and <= 3 ? state.PresetSlot : null;
        var graphicPreset = GraphicPresetCatalog.GetOrDefault(state.GraphicPresetId);
        return state with
        {
            Surface = surface,
            PresetSlot = presetSlot,
            GraphicPresetId = graphicPreset.Id
        };
    }
}
