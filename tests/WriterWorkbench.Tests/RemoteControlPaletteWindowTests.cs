using System.Windows.Controls;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench.Tests;

public sealed class RemoteControlPaletteWindowTests
{
    [Fact]
    public void PaletteRendersProfilePlacementsAsVerticalCommandButtons()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var profile = CreateProfile();
                var palette = new RemoteControlPaletteWindow();

                palette.Render(profile, registry);

                var panel = Assert.IsType<StackPanel>(palette.FindName("RemoteButtonPanel"));
                var buttons = panel.Children.OfType<Button>().ToList();
                var editButton = Assert.IsType<Button>(palette.FindName("RemoteEditButton"));

                Assert.Equal(["project.save", "document.detachCurrent"], buttons.Select(button => button.Tag as string));
                Assert.Equal(
                    ["빠른 저장", "창 옆"],
                    buttons.Select(button => ((StackPanel)button.Content).Children.OfType<TextBlock>().Last().Text));
                Assert.Equal(AppCommandIds.RemoteControlOpenSettings, editButton.Tag);
                Assert.False(palette.ShowInTaskbar);
                palette.Close();
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
    public void PaletteRaisesCommandRequestWhenButtonIsClicked()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var registry = AppCommandCatalog.CreateDefaultRegistry();
                var profile = CreateProfile();
                var palette = new RemoteControlPaletteWindow();
                string? requestedCommandId = null;
                palette.CommandRequested += (_, commandId) => requestedCommandId = commandId;
                palette.Render(profile, registry);

                var panel = Assert.IsType<StackPanel>(palette.FindName("RemoteButtonPanel"));
                var button = panel.Children.OfType<Button>().First();
                button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));

                Assert.Equal(AppCommandIds.ProjectSave, requestedCommandId);
                palette.Close();
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
            "profile-remote-palette",
            "리모콘 팔레트",
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
