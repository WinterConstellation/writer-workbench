namespace WriterWorkbench.Core.Application;

public sealed record AppSessionState(
    string? ProjectRoot,
    string? DocumentId,
    string Surface,
    int? PresetSlot,
    string GraphicPresetId = "default",
    int FocusDurationMinutes = 40,
    RemoteControlSessionState? RemoteControl = null)
{
    public const string EditorSurface = "editor";
    public const string PreviewSurface = "preview";
    public const string MainSurface = "main";
    public const string HtmlWorkbenchSurface = "html-workbench";
    public const string RelationshipMapSurface = "relationship-map";
    public const string DefaultGraphicPresetId = "default";

    public const int DefaultFocusDurationMinutes = 40;

    public static AppSessionState Empty { get; } = new(
        null,
        null,
        HtmlWorkbenchSurface,
        null,
        DefaultGraphicPresetId,
        DefaultFocusDurationMinutes,
        RemoteControlSessionState.Default);
}

public sealed record RemoteControlSessionState(
    bool IsVisible = true,
    bool IsDockedToMemoRail = true,
    string DisplayMode = RemoteControlSessionState.IconAndTitleDisplayMode,
    double? Left = null,
    double? Top = null,
    double Width = RemoteControlSessionState.DefaultWidth,
    double Height = RemoteControlSessionState.DefaultHeight)
{
    public const string IconAndTitleDisplayMode = "iconAndTitle";
    public const string IconOnlyDisplayMode = "iconOnly";
    public const double DefaultWidth = 260;
    public const double DefaultHeight = 450;
    public const double MinWidth = 54;
    public const double MinHeight = 92;
    public const double MaxWidth = 900;
    public const double MaxHeight = 1200;

    public static RemoteControlSessionState Default { get; } = new();
}
