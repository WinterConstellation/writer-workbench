using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;

namespace WriterWorkbench;

public enum RemoteControlDisplayMode
{
    IconAndTitle,
    IconOnly
}

public partial class RemoteControlLayerWindow : Window
{
    public RemoteControlLayerWindow()
    {
        InitializeComponent();
    }

    public event EventHandler<string>? CommandRequested;

    public RemoteControlDisplayMode DisplayMode { get; private set; } = RemoteControlDisplayMode.IconAndTitle;

    public void Render(WorkbenchCustomizationProfile profile, CommandRegistry registry)
    {
        var placements = new WorkbenchCustomizationResolver(profile)
            .GetPlacements("remote", "main");

        if (placements.Count == 0)
        {
            placements = WorkbenchCustomizationProfileFactory.CreateDefaultRemoteControlPlacements(registry);
        }

        RemoteLayerButtonPanel.Children.Clear();
        foreach (var placement in placements)
        {
            var command = registry.Get(placement.CommandId);
            var label = string.IsNullOrWhiteSpace(placement.Label) ? command.Name : placement.Label;
            var button = new System.Windows.Controls.Button
            {
                Content = CreateButtonContent(command.Id, label),
                Tag = command.Id,
                ToolTip = $"{command.Category} / {command.Name}",
                Height = 30,
                MinWidth = 104,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(7, 0, 8, 0),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left
            };
            button.Click += RemoteButton_Click;
            ApplyDisplayMode(button);
            RemoteLayerButtonPanel.Children.Add(button);
        }
    }

    public void SetDisplayMode(RemoteControlDisplayMode mode)
    {
        DisplayMode = mode;
        RemoteLayerDisplayModeButton.Content = mode == RemoteControlDisplayMode.IconAndTitle
            ? "아이콘만"
            : "아이콘+제목";

        foreach (var button in RemoteLayerButtonPanel.Children.OfType<System.Windows.Controls.Button>())
        {
            ApplyDisplayMode(button);
        }
    }

    private void RemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string commandId })
        {
            CommandRequested?.Invoke(this, commandId);
        }
    }

    private void RemoteLayerDragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if the mouse state changes between the event and the call.
        }
    }

    private void RemoteLayerDisplayModeButton_Click(object sender, RoutedEventArgs e)
    {
        SetDisplayMode(DisplayMode == RemoteControlDisplayMode.IconAndTitle
            ? RemoteControlDisplayMode.IconOnly
            : RemoteControlDisplayMode.IconAndTitle);
    }

    private void ApplyDisplayMode(System.Windows.Controls.Button button)
    {
        if (button.Content is not StackPanel panel)
        {
            return;
        }

        var label = panel.Children.OfType<TextBlock>().LastOrDefault();
        if (label is null)
        {
            return;
        }

        if (DisplayMode == RemoteControlDisplayMode.IconOnly)
        {
            label.Visibility = Visibility.Collapsed;
            button.MinWidth = 34;
            button.Width = 34;
            button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
        }
        else
        {
            label.Visibility = Visibility.Visible;
            button.MinWidth = 104;
            button.ClearValue(WidthProperty);
            button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
        }
    }

    private static StackPanel CreateButtonContent(string commandId, string label)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        panel.Children.Add(new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(4),
            Background = CreateIconBrush(commandId),
            Child = new TextBlock
            {
                Text = GetIconText(commandId),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            }
        });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            FontSize = 12
        });

        return panel;
    }

    private static System.Windows.Media.Brush CreateIconBrush(string commandId)
    {
        var color = commandId switch
        {
            AppCommandIds.ProjectSave => System.Windows.Media.Color.FromRgb(37, 99, 235),
            AppCommandIds.DocumentCreateScene => System.Windows.Media.Color.FromRgb(5, 150, 105),
            AppCommandIds.StoryRelationshipMapOpen => System.Windows.Media.Color.FromRgb(219, 39, 119),
            AppCommandIds.ExportCurrentScene or AppCommandIds.ExportFullManuscript => System.Windows.Media.Color.FromRgb(8, 145, 178),
            AppCommandIds.SnapshotCreateCurrent => System.Windows.Media.Color.FromRgb(124, 58, 237),
            AppCommandIds.DocumentDetachCurrent => System.Windows.Media.Color.FromRgb(234, 88, 12),
            _ => System.Windows.Media.Color.FromRgb(75, 85, 99)
        };

        return new SolidColorBrush(color);
    }

    private static string GetIconText(string commandId)
    {
        return commandId switch
        {
            AppCommandIds.ProjectSave => "S",
            AppCommandIds.DocumentCreateScene => "+",
            AppCommandIds.StoryRelationshipMapOpen => "R",
            AppCommandIds.ExportCurrentScene or AppCommandIds.ExportFullManuscript => "T",
            AppCommandIds.SnapshotCreateCurrent => "B",
            AppCommandIds.DocumentDetachCurrent => "W",
            _ => "*"
        };
    }
}
