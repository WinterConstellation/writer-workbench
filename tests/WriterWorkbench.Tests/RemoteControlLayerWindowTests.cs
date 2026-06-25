using System.Windows;
using System.Windows.Controls;
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
                var panel = Assert.IsType<StackPanel>(layer.FindName("RemoteLayerButtonPanel"));
                var buttons = panel.Children.OfType<Button>().ToList();

                Assert.True(layer.Topmost);
                Assert.False(layer.ShowInTaskbar);
                Assert.Equal(WindowStyle.None, layer.WindowStyle);
                Assert.Equal(ResizeMode.NoResize, layer.ResizeMode);
                Assert.Equal(System.Windows.Input.Cursors.SizeAll, handle.Cursor);
                Assert.Equal(["project.save", "document.detachCurrent"], buttons.Select(button => button.Tag as string));
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
