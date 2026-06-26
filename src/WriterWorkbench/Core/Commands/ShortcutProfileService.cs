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
        return CreateDefaultManager(AppCommandCatalog.CreateDefaultRegistry());
    }

    public static ShortcutManager CreateDefaultManager(CommandRegistry commandRegistry)
    {
        var manager = new ShortcutManager();
        foreach (var binding in commandRegistry.DefaultShortcuts)
        {
            Bind(manager, binding);
        }

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
