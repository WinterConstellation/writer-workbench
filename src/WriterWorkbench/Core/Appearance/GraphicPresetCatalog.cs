namespace WriterWorkbench.Core.Appearance;

public static class GraphicPresetCatalog
{
    public const string DefaultId = "default";

    public static IReadOnlyList<GraphicPreset> All { get; } =
    [
        new(
            DefaultId,
            "기본",
            "#FFFFFF",
            "#F5F6F8",
            "#F3F4F6",
            "#FFFFFF",
            "#111827",
            "#4B5563",
            "#D1D5DB",
            "#FFFFFF",
            "#111827",
            "#2563EB"),
        new(
            "dark",
            "검은색 계열",
            "#0B0F14",
            "#111827",
            "#151C24",
            "#0F141B",
            "#E5E7EB",
            "#A7B0BD",
            "#2F3A46",
            "#1F2937",
            "#F9FAFB",
            "#7AA2F7"),
        new(
            "comfort-1",
            "눈이 편한 색상 1",
            "#F6F5EF",
            "#ECE8DC",
            "#EFEBDF",
            "#FBFAF5",
            "#24231F",
            "#625F55",
            "#D7D0C0",
            "#F8F4E8",
            "#24231F",
            "#547A52"),
        new(
            "comfort-2",
            "눈이 편한 색상 2",
            "#F1F5F2",
            "#E4ECE4",
            "#EAF1EA",
            "#FAFCF8",
            "#18251F",
            "#52645A",
            "#C8D6C8",
            "#F4F8F2",
            "#18251F",
            "#3C7C73"),
        new(
            "comfort-3",
            "눈이 편한 색상 3",
            "#F4F1EA",
            "#E8E1D4",
            "#EEE7DB",
            "#FCFAF6",
            "#241F1A",
            "#665C51",
            "#D5C8B5",
            "#F7F1E6",
            "#241F1A",
            "#8A6A3F"),
        new(
            "lavender",
            "라벤더 색상",
            "#F7F4FB",
            "#ECE6F5",
            "#F1EBF8",
            "#FEFCFF",
            "#241E2F",
            "#665A78",
            "#D9CBE8",
            "#F8F2FF",
            "#241E2F",
            "#7C5AA6")
    ];

    public static GraphicPreset GetOrDefault(string? id)
    {
        return All.FirstOrDefault(preset =>
                   string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase))
               ?? All[0];
    }
}
