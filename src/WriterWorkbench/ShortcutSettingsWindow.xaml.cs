using System.Windows;
using WriterWorkbench.Core.Commands;

namespace WriterWorkbench;

public partial class ShortcutSettingsWindow : Window
{
    public ShortcutSettingsWindow(CommandRegistry registry, ShortcutManager manager)
    {
        InitializeComponent();
        Rows = registry.All
            .Select(command => new ShortcutSettingsRow(
                command.Id,
                command.Name,
                command.Category,
                command.DefaultScope,
                manager.Bindings.FirstOrDefault(binding =>
                    string.Equals(binding.CommandId, command.Id, StringComparison.OrdinalIgnoreCase))?.Gesture ?? ""))
            .ToList();
        ShortcutGrid.ItemsSource = Rows;
    }

    public IReadOnlyList<ShortcutSettingsRow> Rows { get; }

    public int RowCount => Rows.Count;

    public ShortcutManager? UpdatedShortcutManager { get; private set; }

    public static ShortcutManager CreateManagerFromRows(IEnumerable<ShortcutSettingsRow> rows, out string? conflict)
    {
        var manager = new ShortcutManager();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Gesture))
            {
                continue;
            }

            var binding = new ShortcutBinding(row.CommandId, row.Gesture.Trim(), row.Scope);
            if (!manager.TryBind(binding, out var conflictCommandId))
            {
                conflict = $"{row.CommandId} 단축키가 {conflictCommandId}와 충돌합니다.";
                return manager;
            }
        }

        conflict = null;
        return manager;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var manager = CreateManagerFromRows(Rows, out var conflict);
        if (conflict is not null)
        {
            ShortcutStatusText.Text = conflict;
            return;
        }

        UpdatedShortcutManager = manager;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
