using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Core.Customization;

public sealed class WorkbenchCustomizationProfileStore(string filePath, CommandRegistry commandRegistry)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<IReadOnlyList<WorkbenchCustomizationProfile>> LoadProfilesAsync(CancellationToken token)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var profiles = await JsonSerializer.DeserializeAsync<List<WorkbenchCustomizationProfile>>(stream, JsonOptions, token) ?? [];
        return profiles
            .Select(NormalizeProfile)
            .Select(profile =>
            {
                ValidateProfile(profile);
                return profile;
            })
            .OrderBy(profile => profile.Name, StringComparer.CurrentCulture)
            .ThenBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveProfileAsync(WorkbenchCustomizationProfile profile, CancellationToken token)
    {
        var normalized = NormalizeProfile(profile);
        ValidateProfile(normalized);

        var profiles = (await LoadProfilesAsync(token)).ToList();
        var index = profiles.FindIndex(existing => string.Equals(existing.Id, normalized.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            profiles[index] = normalized;
        }
        else
        {
            profiles.Add(normalized);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(
            tempPath,
            JsonSerializer.Serialize(
                profiles
                    .OrderBy(saved => saved.Name, StringComparer.CurrentCulture)
                    .ThenBy(saved => saved.Id, StringComparer.OrdinalIgnoreCase),
                JsonOptions),
            token);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private WorkbenchCustomizationProfile NormalizeProfile(WorkbenchCustomizationProfile profile)
    {
        var placements = (profile.Placements ?? [])
            .Select(placement => placement with
            {
                Surface = NormalizeRequired(placement.Surface, "placement surface"),
                Area = NormalizeRequired(placement.Area, "placement area"),
                SlotKey = NormalizeRequired(placement.SlotKey, "placement slot"),
                CommandId = AppCommandIds.NormalizeLegacyId(NormalizeRequired(placement.CommandId, "placement command")),
                Label = (placement.Label ?? "").Trim(),
                Parameters = NormalizeParameters(placement.Parameters)
            })
            .OrderBy(placement => placement.Order)
            .ThenBy(placement => placement.Surface, StringComparer.OrdinalIgnoreCase)
            .ThenBy(placement => placement.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(placement => placement.SlotKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shortcuts = (profile.Shortcuts ?? [])
            .Select(shortcut => shortcut with
            {
                Scope = NormalizeRequired(shortcut.Scope, "shortcut scope"),
                Gesture = NormalizeRequired(shortcut.Gesture, "shortcut gesture"),
                CommandId = AppCommandIds.NormalizeLegacyId(NormalizeRequired(shortcut.CommandId, "shortcut command"))
            })
            .OrderBy(shortcut => shortcut.Scope, StringComparer.OrdinalIgnoreCase)
            .ThenBy(shortcut => shortcut.Gesture, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var macros = (profile.Macros ?? [])
            .Select(macro => macro with
            {
                Id = NormalizeRequired(macro.Id, "macro id"),
                Name = NormalizeRequired(macro.Name, "macro name"),
                Steps = (macro.Steps ?? [])
                    .Select(step => step with
                    {
                        CommandId = AppCommandIds.NormalizeLegacyId(NormalizeRequired(step.CommandId, "macro command")),
                        Parameters = NormalizeParameters(step.Parameters)
                    })
                    .ToList()
            })
            .OrderBy(macro => macro.Name, StringComparer.CurrentCulture)
            .ThenBy(macro => macro.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return profile with
        {
            Id = NormalizeRequired(profile.Id, "profile id"),
            Name = NormalizeRequired(profile.Name, "profile name"),
            Placements = placements,
            Shortcuts = shortcuts,
            Macros = macros
        };
    }

    private void ValidateProfile(WorkbenchCustomizationProfile profile)
    {
        var placementKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var placement in profile.Placements)
        {
            RequireCommand(placement.CommandId);
            var key = $"{placement.Surface}/{placement.Area}/{placement.SlotKey}";
            if (!placementKeys.Add(key))
            {
                throw new InvalidOperationException($"Duplicate command placement slot: {key}");
            }
        }

        var shortcutKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shortcut in profile.Shortcuts)
        {
            RequireCommand(shortcut.CommandId);
            var key = $"{shortcut.Scope}/{shortcut.Gesture}";
            if (!shortcutKeys.Add(key))
            {
                throw new InvalidOperationException($"Duplicate shortcut assignment: {key}");
            }
        }

        var macroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var macro in profile.Macros)
        {
            if (!macroIds.Add(macro.Id))
            {
                throw new InvalidOperationException($"Duplicate macro id: {macro.Id}");
            }

            foreach (var step in macro.Steps)
            {
                RequireCommand(step.CommandId);
            }
        }
    }

    private void RequireCommand(string commandId)
    {
        try
        {
            _ = commandRegistry.Get(commandId);
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException($"Customization profile references unknown command: {commandId}", ex);
        }
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

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length == 0)
        {
            throw new InvalidOperationException($"Customization profile requires {fieldName}.");
        }

        return normalized;
    }
}
