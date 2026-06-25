using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WriterWorkbench.Core.Commands;

public sealed class ShortcutProfileService(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<ShortcutManager> LoadOrCreateDefaultAsync(CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            var defaultManager = CreateDefaultManager();
            await SaveAsync(defaultManager, token);
            return defaultManager;
        }

        await using var stream = File.OpenRead(filePath);
        var bindings = await JsonSerializer.DeserializeAsync<List<ShortcutBinding>>(stream, JsonOptions, token) ?? [];
        var manager = new ShortcutManager();
        foreach (var binding in bindings)
        {
            if (!manager.TryBind(binding, out _))
            {
                continue;
            }
        }

        return manager;
    }

    public async Task SaveAsync(ShortcutManager manager, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(
            tempPath,
            JsonSerializer.Serialize(manager.Bindings, JsonOptions),
            token);
        File.Move(tempPath, filePath, overwrite: true);
    }

    public static ShortcutManager CreateDefaultManager()
    {
        var manager = new ShortcutManager();
        Bind(manager, new ShortcutBinding(AppCommandIds.ProjectSave, "Ctrl+S", CommandScope.Global));
        Bind(manager, new ShortcutBinding(AppCommandIds.AutosaveToggle, "Ctrl+Alt+S", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.WorkspacePresetOne, "Ctrl+Alt+1", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.WorkspacePresetTwo, "Ctrl+Alt+2", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.WorkspacePresetThree, "Ctrl+Alt+3", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.WorkspaceStartupPresetCycle, "Ctrl+Alt+0", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.ShortcutsOpenSettings, "Ctrl+Alt+K", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.ViewPreviewToggle, "Ctrl+Alt+P", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.RemoteControlToggle, "Ctrl+Alt+R", CommandScope.Workbench));
        Bind(manager, new ShortcutBinding(AppCommandIds.HelpOpen, "F1", CommandScope.Global));
        return manager;
    }

    private static void Bind(ShortcutManager manager, ShortcutBinding binding)
    {
        if (!manager.TryBind(binding, out var conflictCommandId))
        {
            throw new InvalidOperationException($"Default shortcut conflict: {binding.CommandId} conflicts with {conflictCommandId}");
        }
    }
}
