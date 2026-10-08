namespace WriterWorkbench.Core.Appearance;

public sealed record GraphicPreset(
    string Id,
    string Name,
    string WindowBackground,
    string ChromeBackground,
    string PanelBackground,
    string EditorBackground,
    string Text,
    string MutedText,
    string Border,
    string ButtonBackground,
    string ButtonText,
    string Accent);
