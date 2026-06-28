using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench.Tests;

public sealed class RemoteControlLayerWindowTests
{
    [Fact]
    public void LayerIsTopmostDraggableAndRendersRemotePlacements()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var layer = new RemoteControlLayerWindow();

                layer.Render(CreateProfile(), registry);

                var handle = Assert.IsType<Border>(layer.FindName("RemoteLayerDragHandle"));
                var frame = Assert.IsType<Border>(layer.FindName("RemoteLayerFrame"));
                var moveIcon = Assert.IsType<Viewbox>(layer.FindName("RemoteLayerMoveIcon"));
                var panel = Assert.IsType<StackPanel>(layer.FindName("RemoteLayerButtonPanel"));
                var buttons = panel.Children.OfType<Button>().ToList();

                Assert.True(layer.Topmost);
                Assert.False(layer.ShowInTaskbar);
                Assert.Equal(WindowStyle.None, layer.WindowStyle);
                Assert.Equal(ResizeMode.CanResizeWithGrip, layer.ResizeMode);
                Assert.Equal(260d, layer.Width);
                Assert.Equal(450d, layer.Height);
                Assert.NotNull(layer.FindName("RemoteLayerResizeGrip"));
                AssertBrushColor("#FFFAF0", frame.Background);
                AssertBrushColor("#9F927F", frame.BorderBrush);
                AssertBrushColor("#E3D8C8", handle.Background);
                AssertBrushColor("#9F927F", handle.BorderBrush);
                Assert.Equal(System.Windows.Input.Cursors.SizeAll, handle.Cursor);
                var moveIconCanvas = Assert.IsType<Canvas>(moveIcon.Child);
                var moveIconPaths = moveIconCanvas.Children.OfType<System.Windows.Shapes.Path>().ToList();
                Assert.True(moveIconPaths.Count >= 2);
                AssertBrushColor("#245C73", moveIconPaths[0].Stroke);
                AssertBrushColor("#245C73", moveIconPaths[1].Fill);
                Assert.Null(layer.FindName("RemoteLayerMoveGlyph"));
                Assert.Equal(["project.save", "document.detachCurrent"], buttons.Select(button => button.Tag as string));
                Assert.All(buttons, button =>
                {
                    Assert.Equal(32d, button.Height);
                    Assert.Equal(new Thickness(0, 0, 0, 6), button.Margin);
                    AssertBrushColor("#FFFAF0", button.Background);
                    AssertBrushColor("#9F927F", button.BorderBrush);
                });
                layer.Left = -900;
                layer.Top = -400;
                Assert.Equal(-900, layer.Left);
                Assert.Equal(-400, layer.Top);
                layer.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void LayerCanSwitchBetweenIconAndTitleAndIconOnlyModes()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var layer = new RemoteControlLayerWindow();
                layer.Render(CreateProfile(), registry);

                var panel = Assert.IsType<StackPanel>(layer.FindName("RemoteLayerButtonPanel"));
                var button = panel.Children.OfType<Button>().First();
                var contentPanel = Assert.IsType<StackPanel>(button.Content);
                var icon = Assert.IsType<Border>(contentPanel.Children[0]);
                var label = contentPanel.Children.OfType<TextBlock>().Last();
                var modeButton = Assert.IsType<Button>(layer.FindName("RemoteLayerDisplayModeButton"));

                Assert.Equal(RemoteControlDisplayMode.IconAndTitle, layer.DisplayMode);
                Assert.Equal(18d, icon.Width);
                Assert.Equal(18d, icon.Height);
                Assert.Equal(new Thickness(7, 0, 0, 0), label.Margin);
                Assert.Equal(Visibility.Visible, label.Visibility);
                Assert.Equal(HorizontalAlignment.Stretch, button.HorizontalAlignment);
                Assert.Equal("아이콘만", modeButton.Content);

                layer.SetDisplayMode(RemoteControlDisplayMode.IconOnly);

                Assert.Equal(RemoteControlDisplayMode.IconOnly, layer.DisplayMode);
                Assert.Equal(Visibility.Collapsed, label.Visibility);
                Assert.Equal(40d, button.Width);
                Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
                Assert.Equal("아이콘+제목", modeButton.Content);
                layer.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    [Fact]
    public void LayerRaisesCommandRequestWhenButtonIsClicked()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var layer = new RemoteControlLayerWindow();
                string? requestedCommandId = null;
                layer.CommandRequested += (_, commandId) => requestedCommandId = commandId;
                layer.Render(CreateProfile(), registry);

                var panel = Assert.IsType<StackPanel>(layer.FindName("RemoteLayerButtonPanel"));
                var button = panel.Children.OfType<Button>().First();
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                Assert.Equal(AppCommandIds.ProjectSave, requestedCommandId);
                layer.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw failure;
        }
    }

    private static void AssertBrushColor(string expectedHex, Brush brush)
    {
        var solid = Assert.IsType<SolidColorBrush>(brush);
        Assert.Equal((Color)ColorConverter.ConvertFromString(expectedHex), solid.Color);
    }

    private static WorkbenchCustomizationProfile CreateProfile()
    {
        var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
        return new WorkbenchCustomizationProfile(
            "profile-remote-layer",
            "리모콘 레이어",
            [
                new CommandPlacement("remote", "main", "second", AppCommandIds.DocumentDetachCurrent, "창 옆", 20, new Dictionary<string, string>()),
                new CommandPlacement("remote", "main", "first", AppCommandIds.ProjectSave, "빠른 저장", 10, new Dictionary<string, string>())
            ],
            [],
            [],
            now,
            now);
    }
}
