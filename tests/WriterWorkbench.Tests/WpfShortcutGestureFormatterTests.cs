using System.Windows.Input;

namespace WriterWorkbench.Tests;

public sealed class WpfShortcutGestureFormatterTests
{
    [Fact]
    public void FormatsControlAltDigitShortcut()
    {
        var gesture = WpfShortcutGestureFormatter.Format(Key.D2, ModifierKeys.Control | ModifierKeys.Alt);

        Assert.Equal("Ctrl+Alt+2", gesture);
    }

    [Fact]
    public void FormatsControlLetterShortcut()
    {
        var gesture = WpfShortcutGestureFormatter.Format(Key.S, ModifierKeys.Control);

        Assert.Equal("Ctrl+S", gesture);
    }

    [Fact]
    public void IgnoresBareModifierKey()
    {
        var gesture = WpfShortcutGestureFormatter.Format(Key.LeftCtrl, ModifierKeys.Control);

        Assert.Null(gesture);
    }
}
