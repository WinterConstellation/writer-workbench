using WriterWorkbench.Core.Appearance;

namespace WriterWorkbench.Tests;

public sealed class GraphicPresetCatalogTests
{
    [Fact]
    public void ContainsRequestedGraphicPresets()
    {
        var names = GraphicPresetCatalog.All.Select(preset => preset.Name).ToList();

        Assert.Equal(
            ["기본", "검은색 계열", "눈이 편한 색상 1", "눈이 편한 색상 2", "눈이 편한 색상 3", "라벤더 색상"],
            names);
        Assert.Equal(GraphicPresetCatalog.All.Count, GraphicPresetCatalog.All.Select(preset => preset.Id).Distinct().Count());
    }

    [Fact]
    public void FallsBackToDefaultPreset()
    {
        var preset = GraphicPresetCatalog.GetOrDefault("missing");

        Assert.Equal(GraphicPresetCatalog.DefaultId, preset.Id);
        Assert.Equal("기본", preset.Name);
    }
}
