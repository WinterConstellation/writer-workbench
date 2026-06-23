using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WriterWorkbench.Core.Workspace;

public sealed class WorkspacePresetService(string? filePath = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<int, WorkspacePreset> _presets = [];

    public void Save(WorkspacePreset preset)
    {
        if (preset.Slot is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(preset), "MVP supports preset slots 1, 2, and 3.");
        }

        _presets[preset.Slot] = preset;
    }

    public async Task SaveAsync(WorkspacePreset preset, CancellationToken token)
    {
        Save(preset);
        await PersistAsync(token);
    }

    public WorkspacePreset? Get(int slot)
    {
        return _presets.GetValueOrDefault(slot);
    }

    public IReadOnlyList<WorkspacePreset> GetAll()
    {
        return _presets.Values.OrderBy(preset => preset.Slot).ToList();
    }

    public WorkspacePreset? GetStartupPreset()
    {
        return _presets.Values
            .Where(preset => preset.AutoApplyOnStartup)
            .OrderBy(preset => preset.Slot)
            .FirstOrDefault();
    }

    public async Task SetStartupPresetAsync(int? slot, CancellationToken token)
    {
        if (slot is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "MVP supports preset slots 1, 2, and 3.");
        }

        foreach (var preset in _presets.Values.ToList())
        {
            _presets[preset.Slot] = preset with { AutoApplyOnStartup = preset.Slot == slot };
        }

        await PersistAsync(token);
    }

    public async Task LoadAsync(CancellationToken token)
    {
        if (filePath is null || !File.Exists(filePath))
        {
            return;
        }

        await using var stream = File.OpenRead(filePath);
        var presets = await JsonSerializer.DeserializeAsync<List<WorkspacePreset>>(stream, JsonOptions, token) ?? [];
        _presets.Clear();
        foreach (var preset in presets)
        {
            Save(preset);
        }
    }

    private async Task PersistAsync(CancellationToken token)
    {
        if (filePath is null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(
            tempPath,
            JsonSerializer.Serialize(_presets.Values.OrderBy(preset => preset.Slot), JsonOptions),
            token);
        File.Move(tempPath, filePath, overwrite: true);
    }
}
