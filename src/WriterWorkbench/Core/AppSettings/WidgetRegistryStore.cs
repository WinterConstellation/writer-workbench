using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench.Core.AppSettings;

public sealed class WidgetRegistryStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<WorkbenchWidgetRegistry> LoadOrCreateAsync(
        IReadOnlyList<CommandPlacement> defaultPlacements,
        CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            var created = new WorkbenchWidgetRegistry(
                "widget-registry",
                defaultPlacements.Select(ToWidgetInstance).ToList());
            await SaveAsync(created, token);
            return created;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return Normalize(
                await JsonSerializer.DeserializeAsync<WorkbenchWidgetRegistry>(stream, JsonOptions, token)
                ?? WorkbenchWidgetRegistry.Empty);
        }
        catch (JsonException)
        {
            return WorkbenchWidgetRegistry.Empty;
        }
        catch (IOException)
        {
            return WorkbenchWidgetRegistry.Empty;
        }
    }

    public async Task SaveAsync(WorkbenchWidgetRegistry registry, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(
            tempPath,
            JsonSerializer.Serialize(Normalize(registry), JsonOptions),
            token);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private static WidgetInstance ToWidgetInstance(CommandPlacement placement)
    {
        return new WidgetInstance(
            $"widget-{placement.Surface}-{placement.Area}-{placement.SlotKey}".Replace('.', '-').Replace('/', '-'),
            "command-button",
            placement.Surface,
            placement.Area,
            placement.SlotKey,
            placement.Order,
            placement.CommandId,
            placement.Label,
            placement.Parameters);
    }

    private static WorkbenchWidgetRegistry Normalize(WorkbenchWidgetRegistry registry)
    {
        return new WorkbenchWidgetRegistry(
            NormalizeOrDefault(registry.Schema, "widget-registry"),
            (registry.Instances ?? [])
                .Select(NormalizeInstance)
                .OrderBy(instance => instance.Surface, StringComparer.OrdinalIgnoreCase)
                .ThenBy(instance => instance.Area, StringComparer.OrdinalIgnoreCase)
                .ThenBy(instance => instance.Order)
                .ThenBy(instance => instance.SlotKey, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static WidgetInstance NormalizeInstance(WidgetInstance instance)
    {
        return instance with
        {
            Id = NormalizeOrDefault(instance.Id, $"widget-{instance.CommandId}"),
            WidgetId = NormalizeOrDefault(instance.WidgetId, "command-button"),
            Surface = NormalizeOrDefault(instance.Surface, "toolbar"),
            Area = NormalizeOrDefault(instance.Area, "main"),
            SlotKey = NormalizeOrDefault(instance.SlotKey, instance.CommandId),
            CommandId = NormalizeOrDefault(instance.CommandId, ""),
            Label = instance.Label?.Trim() ?? "",
            Parameters = NormalizeParameters(instance.Parameters)
        };
    }

    private static Dictionary<string, string> NormalizeParameters(Dictionary<string, string>? parameters)
    {
        return (parameters ?? [])
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                pair => pair.Key.Trim(),
                pair => pair.Value?.Trim() ?? "",
                StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        var normalized = value?.Trim() ?? "";
        return normalized.Length == 0 ? fallback : normalized;
    }
}
