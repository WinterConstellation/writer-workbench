using System.Windows;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench;

public partial class RemoteControlSettingsWindow : Window
{
    private readonly CommandRegistry _registry;
    private readonly WorkbenchCustomizationProfile _profile;

    public RemoteControlSettingsWindow(CommandRegistry registry, WorkbenchCustomizationProfile profile)
    {
        InitializeComponent();
        _registry = registry;
        _profile = profile;
        Rows = CreateRows(registry, profile);
        RemoteControlGrid.ItemsSource = Rows;
        RemoteControlStatusText.Text = $"리모콘 바로가기 {Rows.Count(row => row.ShowInRemote):N0}개 선택됨";
    }

    public IReadOnlyList<RemoteControlSettingsRow> Rows { get; }

    public int RowCount => Rows.Count;

    public WorkbenchCustomizationProfile? UpdatedProfile { get; private set; }

    public static WorkbenchCustomizationProfile ApplyRemoteRows(
        WorkbenchCustomizationProfile profile,
        IEnumerable<RemoteControlSettingsRow> rows,
        CommandRegistry registry,
        DateTimeOffset updatedAt)
    {
        var keptPlacements = profile.Placements
            .Where(placement =>
                !string.Equals(placement.Surface, "remote", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(placement.Area, "main", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var remoteRows = rows
            .Where(row => row.ShowInRemote)
            .OrderBy(row => row.Order <= 0 ? int.MaxValue : row.Order)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ThenBy(row => row.CommandId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var index = 0; index < remoteRows.Count; index++)
        {
            var row = remoteRows[index];
            var command = registry.Get(row.CommandId);
            var label = string.IsNullOrWhiteSpace(row.Label) ? command.Name : row.Label.Trim();
            keptPlacements.Add(new CommandPlacement(
                "remote",
                "main",
                $"remote-{index + 1:00}",
                command.Id,
                label,
                index + 1,
                new Dictionary<string, string>()));
        }

        return profile with
        {
            Placements = keptPlacements,
            UpdatedAt = updatedAt
        };
    }

    private static IReadOnlyList<RemoteControlSettingsRow> CreateRows(
        CommandRegistry registry,
        WorkbenchCustomizationProfile profile)
    {
        var existing = new WorkbenchCustomizationResolver(profile)
            .GetPlacements("remote", "main")
            .GroupBy(placement => placement.CommandId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var fallbackOrder = existing.Count + 1;

        return registry.All
            .Select(command =>
            {
                existing.TryGetValue(command.Id, out var placement);
                return new RemoteControlSettingsRow(
                    command.Id,
                    command.Name,
                    command.Category,
                    placement is not null,
                    placement?.Order ?? fallbackOrder++,
                    placement?.Label ?? command.Name);
            })
            .OrderBy(row => row.ShowInRemote ? 0 : 1)
            .ThenBy(row => row.Order)
            .ThenBy(row => row.Category, StringComparer.CurrentCulture)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        UpdatedProfile = ApplyRemoteRows(_profile, Rows, _registry, DateTimeOffset.UtcNow);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public sealed class RemoteControlSettingsRow(
    string commandId,
    string name,
    string category,
    bool showInRemote,
    int order,
    string label)
{
    public string CommandId { get; } = commandId;

    public string Name { get; } = name;

    public string Category { get; } = category;

    public bool ShowInRemote { get; set; } = showInRemote;

    public int Order { get; set; } = order;

    public string Label { get; set; } = label;
}
