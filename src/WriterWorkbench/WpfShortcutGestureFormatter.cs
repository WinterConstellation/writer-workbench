using System.Windows.Input;

namespace WriterWorkbench;

public static class WpfShortcutGestureFormatter
{
    public static string? Format(Key key, ModifierKeys modifiers)
    {
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return null;
        }

        var parts = new List<string>();
        if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            parts.Add("Alt");
        }

        if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            parts.Add("Shift");
        }

        var keyText = FormatKey(key);
        if (keyText is null)
        {
            return null;
        }

        parts.Add(keyText);
        return string.Join("+", parts);
    }

    private static string? FormatKey(Key key)
    {
        if (key is >= Key.D0 and <= Key.D9)
        {
            return ((int)key - (int)Key.D0).ToString();
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return ((int)key - (int)Key.NumPad0).ToString();
        }

        if (key is >= Key.A and <= Key.Z)
        {
            return key.ToString();
        }

        if (key is >= Key.F1 and <= Key.F24)
        {
            return key.ToString();
        }

        return key switch
        {
            Key.Escape => "Esc",
            Key.Enter => "Enter",
            Key.Tab => "Tab",
            Key.Space => "Space",
            Key.Delete => "Delete",
            Key.Back => "Backspace",
            _ => null
        };
    }
}
