namespace WriterWorkbench.Core.Application;

public sealed record AppSessionState(
    string? ProjectRoot,
    string? DocumentId,
    string Surface,
    int? PresetSlot,
    string GraphicPresetId = "default")
{
    public const string EditorSurface = "editor";
    public const string PreviewSurface = "preview";
    public const string MainSurface = "main";
    public const string DefaultGraphicPresetId = "default";

    public static AppSessionState Empty { get; } = new(null, null, EditorSurface, null, DefaultGraphicPresetId);
}
