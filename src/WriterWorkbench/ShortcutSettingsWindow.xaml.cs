using System.Windows;
using System.Windows.Input;
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

            if (!TryNormalizeGestureText(row.Gesture, out var gesture))
            {
                conflict = $"{row.CommandId} 단축키가 올바르지 않습니다: {row.Gesture}";
                return manager;
            }

            var binding = new ShortcutBinding(row.CommandId, gesture, row.Scope);
            if (!manager.TryBind(binding, out var conflictCommandId))
            {
                conflict = $"{row.CommandId} 단축키가 {conflictCommandId}와 충돌합니다.";
                return manager;
            }
        }

        conflict = null;
        return manager;
    }

    public static string? CreateGestureTextForKey(
        Key key,
        Key systemKey,
        Key imeProcessedKey,
        ModifierKeys modifiers)
    {
        var resolvedKey = ResolveShortcutKey(key, systemKey, imeProcessedKey);
        if (resolvedKey is Key.None)
        {
            return null;
        }

        if ((resolvedKey is Key.Back or Key.Delete) && modifiers == ModifierKeys.None)
        {
            return "";
        }

        var gesture = WpfShortcutGestureFormatter.Format(resolvedKey, modifiers);
        return gesture is not null && TryNormalizeGestureText(gesture, out var normalized)
            ? normalized
            : null;
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

    private void ShortcutGestureBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        var gesture = CreateGestureTextForKey(e.Key, e.SystemKey, e.ImeProcessedKey, Keyboard.Modifiers);
        e.Handled = true;

        if (gesture is null)
        {
            ShortcutStatusText.Text = "Ctrl/Alt/Shift와 글자, 숫자, F키를 함께 누르세요. Backspace/Delete는 비우기입니다.";
            return;
        }

        textBox.Text = gesture;
        textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        ShortcutStatusText.Text = string.IsNullOrEmpty(gesture)
            ? "단축키 비움"
            : $"단축키 지정됨 - {gesture}";
    }

    private static Key ResolveShortcutKey(Key key, Key systemKey, Key imeProcessedKey)
    {
        if (key == Key.System)
        {
            return systemKey;
        }

        if (key == Key.ImeProcessed)
        {
            return imeProcessedKey;
        }

        return key;
    }

    private static bool TryNormalizeGestureText(string gesture, out string normalized)
    {
        normalized = "";
        var parts = gesture.Split(
                '+',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (parts.Count == 0)
        {
            return false;
        }

        var modifiers = new List<string>();
        string? keyPart = null;
        foreach (var part in parts)
        {
            var normalizedPart = NormalizeGesturePart(part);
            if (normalizedPart is "Ctrl" or "Alt" or "Shift")
            {
                if (keyPart is not null || modifiers.Contains(normalizedPart, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                modifiers.Add(normalizedPart);
                continue;
            }

            if (keyPart is not null)
            {
                return false;
            }

            keyPart = NormalizeShortcutKeyPart(normalizedPart);
            if (keyPart is null)
            {
                return false;
            }
        }

        if (keyPart is null)
        {
            return false;
        }

        if (modifiers.Count == 0 && !IsFunctionKey(keyPart))
        {
            return false;
        }

        var orderedModifiers = new[] { "Ctrl", "Alt", "Shift" }
            .Where(modifier => modifiers.Contains(modifier, StringComparer.OrdinalIgnoreCase));
        normalized = string.Join("+", orderedModifiers.Append(keyPart));
        return true;
    }

    private static string NormalizeGesturePart(string part)
    {
        return part.Equals("Control", StringComparison.OrdinalIgnoreCase)
            ? "Ctrl"
            : part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
    }

    private static string? NormalizeShortcutKeyPart(string part)
    {
        if (part.Length == 1 && char.IsLetter(part[0]))
        {
            return part.ToUpperInvariant();
        }

        if (part.Length == 1 && char.IsDigit(part[0]))
        {
            return part;
        }

        if (IsFunctionKey(part))
        {
            return part.ToUpperInvariant();
        }

        return part.Equals("Esc", StringComparison.OrdinalIgnoreCase) ? "Esc" :
            part.Equals("Enter", StringComparison.OrdinalIgnoreCase) ? "Enter" :
            part.Equals("Tab", StringComparison.OrdinalIgnoreCase) ? "Tab" :
            part.Equals("Space", StringComparison.OrdinalIgnoreCase) ? "Space" :
            part.Equals("Delete", StringComparison.OrdinalIgnoreCase) ? "Delete" :
            part.Equals("Backspace", StringComparison.OrdinalIgnoreCase) ? "Backspace" :
            null;
    }

    private static bool IsFunctionKey(string key)
    {
        return key.Length is >= 2 and <= 3 &&
            key[0] is 'F' or 'f' &&
            int.TryParse(key[1..], out var number) &&
            number is >= 1 and <= 24;
    }
}
