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
            var state = await JsonSerializer.DeserializeAsync<AppSessionState>(stream, JsonOptions, token);
            return state is null ? AppSessionState.Empty : Normalize(state);
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
        var surface = StartupSurfaceResolver.NormalizeSurface(state.Surface);

        var presetSlot = state.PresetSlot is >= 1 and <= 3 ? state.PresetSlot : null;
        var graphicPreset = GraphicPresetCatalog.GetOrDefault(state.GraphicPresetId);
        var focusDurationMinutes = Math.Clamp(
            state.FocusDurationMinutes,
            1,
            240);
        return state with
        {
            Surface = surface,
            PresetSlot = presetSlot,
            GraphicPresetId = graphicPreset.Id,
            FocusDurationMinutes = focusDurationMinutes,
            RemoteControl = NormalizeRemoteControl(state.RemoteControl)
        };
    }

    private static RemoteControlSessionState NormalizeRemoteControl(RemoteControlSessionState? state)
    {
        var current = state ?? RemoteControlSessionState.Default;
        var displayMode = string.Equals(
            current.DisplayMode,
            RemoteControlSessionState.IconOnlyDisplayMode,
            StringComparison.OrdinalIgnoreCase)
            ? RemoteControlSessionState.IconOnlyDisplayMode
            : RemoteControlSessionState.IconAndTitleDisplayMode;
        var isDocked = current.IsDockedToMemoRail;
        return current with
        {
            IsDockedToMemoRail = isDocked,
            DisplayMode = displayMode,
            Left = isDocked ? null : NormalizeCoordinate(current.Left),
            Top = isDocked ? null : NormalizeCoordinate(current.Top),
            Width = NormalizeSize(
                current.Width,
                RemoteControlSessionState.MinWidth,
                RemoteControlSessionState.MaxWidth,
                RemoteControlSessionState.DefaultWidth),
            Height = NormalizeSize(
                current.Height,
                RemoteControlSessionState.MinHeight,
                RemoteControlSessionState.MaxHeight,
                RemoteControlSessionState.DefaultHeight)
        };
    }

    private static double? NormalizeCoordinate(double? value)
    {
        return value is double coordinate && double.IsFinite(coordinate)
            ? coordinate
            : null;
    }

    private static double NormalizeSize(double value, double minimum, double maximum, double fallback)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            return fallback;
        }

        return Math.Clamp(value, minimum, maximum);
    }
}
