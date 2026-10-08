namespace WriterWorkbench.Core.AppSettings;

public sealed record WorkbenchWidgetRegistry(
    string Schema,
    IReadOnlyList<WidgetInstance> Instances)
{
    public static WorkbenchWidgetRegistry Empty { get; } = new("widget-registry", []);
}

public sealed record WidgetInstance(
    string Id,
    string WidgetId,
    string Surface,
    string Area,
    string SlotKey,
    int Order,
    string CommandId,
    string Label,
    Dictionary<string, string> Parameters);
