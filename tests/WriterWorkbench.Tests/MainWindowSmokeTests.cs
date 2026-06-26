using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Workspace;
using Xunit.Abstractions;

namespace WriterWorkbench.Tests;

public sealed class MainWindowSmokeTests
{
    private readonly ITestOutputHelper _output;

    public MainWindowSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void MainWindowConstructsOnStaThread()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                Assert.Equal("원고 작업대", window.Title);
                window.Close();
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
    public void MainWindowContainsWorkspacePresetCommands()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var commandTags = FindCommandTags(window)
                    .Where(tag => tag is not null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Assert.Contains(AppCommandIds.WorkspacePresetOne, commandTags);
                Assert.Contains(AppCommandIds.WorkspacePresetTwo, commandTags);
                Assert.Contains(AppCommandIds.WorkspacePresetThree, commandTags);
                Assert.Contains(AppCommandIds.WorkspaceStartupPresetCycle, commandTags);
                Assert.Contains(AppCommandIds.ShortcutsOpenSettings, commandTags);
                Assert.Contains(AppCommandIds.ViewHtmlWorkbenchOpen, commandTags);
                Assert.Contains(AppCommandIds.ViewMainOpen, commandTags);
                Assert.Contains(AppCommandIds.ViewPreviewToggle, commandTags);
                Assert.Contains(AppCommandIds.HelpOpen, commandTags);
                Assert.Contains(AppCommandIds.ExportCurrentScene, commandTags);
                Assert.Contains(AppCommandIds.ExportFullManuscript, commandTags);
                Assert.Contains(AppCommandIds.SnapshotCreateCurrent, commandTags);
                Assert.Contains(AppCommandIds.SnapshotRestoreSelected, commandTags);
                Assert.Contains(AppCommandIds.SnapshotDeleteSelected, commandTags);
                Assert.Contains(AppCommandIds.StoryRelationshipMapOpen, commandTags);
                Assert.Contains(AppCommandIds.StoryAddNode, commandTags);
                Assert.Contains(AppCommandIds.StoryUpdateNode, commandTags);
                Assert.Contains(AppCommandIds.StoryDeleteNode, commandTags);
                Assert.Contains(AppCommandIds.StoryAddRelationship, commandTags);
                Assert.Contains(AppCommandIds.StoryUpdateRelationship, commandTags);
                Assert.Contains(AppCommandIds.StoryDeleteRelationship, commandTags);
                Assert.Contains(AppCommandIds.SceneEntityLinkAdd, commandTags);
                Assert.Contains(AppCommandIds.SceneEntityLinkDelete, commandTags);
                Assert.Contains(AppCommandIds.DocumentRenameScene, commandTags);
                Assert.Contains(AppCommandIds.DocumentDuplicateScene, commandTags);
                Assert.Contains(AppCommandIds.DocumentDeleteScene, commandTags);
                Assert.Contains(AppCommandIds.DocumentMoveSceneUp, commandTags);
                Assert.Contains(AppCommandIds.DocumentMoveSceneDown, commandTags);
                window.Close();
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
    public void MainWindowBinderHasRightClickSceneActionMenu()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var binder = Assert.IsType<ListBox>(window.FindName("BinderList"));
                Assert.NotNull(binder.ContextMenu);
                var contextMenu = binder.ContextMenu;
                var menuItems = contextMenu.Items.OfType<MenuItem>().ToList();
                Assert.NotNull(binder.ItemContainerStyle);
                var eventSetters = binder.ItemContainerStyle
                    .Setters
                    .OfType<EventSetter>()
                    .ToList();

                Assert.Equal(
                    [
                        AppCommandIds.DocumentRenameScene,
                        AppCommandIds.DocumentDuplicateScene,
                        AppCommandIds.DocumentDeleteScene,
                        AppCommandIds.DocumentMoveSceneUp,
                        AppCommandIds.DocumentMoveSceneDown,
                        AppCommandIds.SnapshotCreateCurrent,
                        AppCommandIds.ExportCurrentScene,
                        AppCommandIds.DocumentDetachCurrent
                    ],
                    menuItems.Select(item => item.Tag as string));
                Assert.All(menuItems, item => Assert.Same(contextMenu, item.Parent));
                Assert.Contains(eventSetters, setter => setter.Event.Name == "PreviewMouseRightButtonDown");
                window.Close();
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
    public void MainWindowContainsIconMenuAndRemoteControlEntryPoint()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();

                var menu = Assert.IsType<Menu>(window.FindName("WorkbenchMenuBar"));
                var menuItems = menu.Items.OfType<MenuItem>().ToList();
                Assert.Equal(
                    [
                        "menu.file",
                        "menu.edit",
                        "menu.manuscript",
                        "menu.story",
                        "menu.view",
                        "menu.window",
                        "menu.tools",
                        "menu.help"
                    ],
                    menuItems.Select(item => item.Tag as string));
                Assert.All(menuItems, item => Assert.NotNull(item.Icon));

                Assert.Null(window.FindName("RemoteControlBar"));
                Assert.Null(window.FindName("RemoteControlFloatingPanel"));
                Assert.Null(window.FindName("RemoteFloatingButtonPanel"));
                Assert.Contains(AppCommandIds.RemoteControlShow, FindCommandTags(window).Where(tag => tag is not null));
                Assert.Contains(AppCommandIds.RemoteControlToggle, FindCommandTags(window).Where(tag => tag is not null));
                Assert.Contains(AppCommandIds.RemoteControlOpenSettings, FindCommandTags(window).Where(tag => tag is not null));
                window.Close();
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
    public void MainWindowRemoteControlToggleShowsAndHidesLayer()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(layerField);

                InvokeCommand(window, AppCommandIds.RemoteControlToggle);
                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                Assert.True(layer.IsVisible);

                InvokeCommand(window, AppCommandIds.RemoteControlToggle);
                Assert.False(layer.IsVisible);

                layer.Close();
                window.Close();
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
    public void MainWindowRendersTopmostMovableRemoteControlLayerFromCustomizationProfile()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
                var profile = new WorkbenchCustomizationProfile(
                    "profile-remote-test",
                    "리모콘 테스트",
                    [
                        new CommandPlacement("remote", "main", "second", AppCommandIds.ProjectSave, "저장 고정", 20, new Dictionary<string, string>()),
                        new CommandPlacement("remote", "main", "first", AppCommandIds.DocumentDetachCurrent, "분리 고정", 10, new Dictionary<string, string>()),
                        new CommandPlacement("toolbar", "main", "ignored", AppCommandIds.HelpOpen, "무시", 30, new Dictionary<string, string>())
                    ],
                    [],
                    [],
                    now,
                    now);
                var method = typeof(MainWindow).GetMethod(
                    "RenderRemoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(method);
                Assert.NotNull(layerField);
                method!.Invoke(window, [profile]);

                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                var panel = Assert.IsType<StackPanel>(layer.FindName("RemoteLayerButtonPanel"));
                var buttons = panel.Children.OfType<Button>().ToList();

                Assert.True(layer.Topmost);
                Assert.False(layer.ShowInTaskbar);
                Assert.Equal(WindowStyle.None, layer.WindowStyle);
                Assert.NotNull(layer.FindName("RemoteLayerDragHandle"));
                Assert.Equal(["document.detachCurrent", "project.save"], buttons.Select(button => button.Tag as string));
                Assert.Equal(
                    ["분리 고정", "저장 고정"],
                    buttons.Select(button => ((StackPanel)button.Content).Children.OfType<TextBlock>().Last().Text));
                layer.Left = -500;
                layer.Top = -300;
                Assert.Equal(-500, layer.Left);
                Assert.Equal(-300, layer.Top);
                layer.Close();
                window.Close();
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
    public void MainWindowContainsLongOperationProgressSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();

                Assert.NotNull(window.FindName("EditorSurface"));
                Assert.NotNull(window.FindName("HtmlWorkbenchSurface"));
                Assert.NotNull(window.FindName("HtmlWorkbenchBrowser"));
                Assert.NotNull(window.FindName("PreviewSurface"));
                Assert.NotNull(window.FindName("MainSurface"));
                Assert.NotNull(window.FindName("MainRecentList"));
                var graphicPresetBox = Assert.IsType<ComboBox>(window.FindName("GraphicPresetBox"));
                Assert.Equal(6, graphicPresetBox.Items.Count);
                Assert.NotNull(window.FindName("PreviewModeButton"));
                Assert.NotNull(window.FindName("PreviewText"));
                Assert.NotNull(window.FindName("OperationProgressPanel"));
                Assert.NotNull(window.FindName("OperationProgressBar"));
                Assert.NotNull(window.FindName("OperationRemainingGraph"));
                window.Close();
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
    public void MainWindowCanShowHtmlWorkbenchSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var htmlSurface = Assert.IsType<DockPanel>(window.FindName("HtmlWorkbenchSurface"));
                var editorSurface = Assert.IsType<DockPanel>(window.FindName("EditorSurface"));

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");

                Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, editorSurface.Visibility);
                window.Close();
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
    public void MainWindowContainsCustomFocusDurationControl()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var focusDuration = Assert.IsType<TextBox>(window.FindName("FocusDurationMinutesBox"));
                var focusButton = Assert.IsType<Button>(window.FindName("FocusButton"));

                Assert.Equal("40", focusDuration.Text);
                Assert.Equal("집중 40:00", focusButton.Content);
                window.Close();
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
    public void MainWindowStartsFocusWithCustomDurationMinutes()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var focusDuration = Assert.IsType<TextBox>(window.FindName("FocusDurationMinutesBox"));
                var endsAtField = typeof(MainWindow).GetField(
                    "_focusEndsAt",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(endsAtField);
                focusDuration.Text = "25";

                var before = DateTimeOffset.Now;
                InvokeCommand(window, AppCommandIds.WritingFocusToggle);
                var endsAt = Assert.IsType<DateTimeOffset>(endsAtField!.GetValue(window));
                var remaining = endsAt - before;

                Assert.InRange(remaining.TotalMinutes, 24.5, 25.5);
                InvokePrivate(window, "ExitFocus");
                window.Close();
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
    public void MainWindowRendersMainCommandGridFromCustomizationProfile()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var now = DateTimeOffset.Parse("2026-06-25T00:00:00+09:00");
                var profile = new WorkbenchCustomizationProfile(
                    "profile-ui-test",
                    "테스트 작업대",
                    [
                        new CommandPlacement("toolbar", "main", "second", AppCommandIds.ViewMainOpen, "메인", 20, new Dictionary<string, string>()),
                        new CommandPlacement("toolbar", "main", "first", AppCommandIds.ProjectSave, "저장", 10, new Dictionary<string, string>()),
                        new CommandPlacement("panel", "right", "ignored", AppCommandIds.HelpOpen, "도움말", 30, new Dictionary<string, string>())
                    ],
                    [],
                    [],
                    now,
                    now);
                var method = typeof(MainWindow).GetMethod(
                    "RenderMainCommandGrid",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(method);
                method!.Invoke(window, [profile]);

                var grid = Assert.IsType<UniformGrid>(window.FindName("MainCommandGrid"));
                var buttons = grid.Children.OfType<Button>().ToList();

                Assert.Equal(2, grid.Columns);
                Assert.Equal(1, grid.Rows);
                Assert.Equal(["project.save", "view.main.open"], buttons.Select(button => button.Tag as string));
                Assert.Equal(["저장", "메인"], buttons.Select(button => button.Content as string));
                window.Close();
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
    public void MainWindowClaimedEditorSurfaceDisablesEditorInDetachedWorkbench()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var showEditor = typeof(MainWindow).GetMethod(
                    "ShowEditorSurface",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var registryField = typeof(MainWindow).GetField(
                    "_surfaceClaims",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(showEditor);
                Assert.NotNull(registryField);

                showEditor!.Invoke(window, []);
                var registry = Assert.IsType<WorkbenchSurfaceClaimRegistry>(registryField!.GetValue(window));
                var detached = new WorkbenchDetachedWindow(registry, "detached-main-window-test");
                var editorButton = Assert.IsType<Button>(detached.FindName("DetachedEditorSurfaceButton"));

                Assert.True(registry.IsClaimedBy(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface));
                Assert.False(editorButton.IsEnabled);
                detached.Close();
                window.Close();
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
    public void MainWindowContainsStoryStructureSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();

                Assert.NotNull(window.FindName("StoryStructurePanel"));
                Assert.NotNull(window.FindName("StoryNodeNameBox"));
                Assert.NotNull(window.FindName("StoryNodeKindBox"));
                Assert.NotNull(window.FindName("StoryNodeSummaryBox"));
                Assert.NotNull(window.FindName("StoryNodeList"));
                Assert.NotNull(window.FindName("StoryRelationshipSourceBox"));
                Assert.NotNull(window.FindName("StoryRelationshipTargetBox"));
                Assert.NotNull(window.FindName("StoryRelationshipKindBox"));
                Assert.NotNull(window.FindName("StoryRelationshipSummaryBox"));
                Assert.NotNull(window.FindName("StoryRelationshipList"));
                Assert.NotNull(window.FindName("RelationshipMapSurface"));
                Assert.NotNull(window.FindName("RelationshipEntityList"));
                Assert.NotNull(window.FindName("RelationshipList"));
                Assert.NotNull(window.FindName("RelationshipMapCanvas"));
                Assert.NotNull(window.FindName("RelationshipEntityNameBox"));
                Assert.NotNull(window.FindName("RelationshipEntityTypeBox"));
                Assert.NotNull(window.FindName("RelationshipEntityRoleBox"));
                Assert.NotNull(window.FindName("RelationshipEntitySummaryBox"));
                Assert.NotNull(window.FindName("RelationshipSourceBox"));
                Assert.NotNull(window.FindName("RelationshipTargetBox"));
                Assert.NotNull(window.FindName("RelationshipLabelBox"));
                Assert.NotNull(window.FindName("RelationshipNotesBox"));
                Assert.NotNull(window.FindName("RelationshipDirectionalBox"));
                window.Close();
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
    public void RelationshipMapCommandAddsEntitiesRelationshipAndRendersMap()
    {
        string? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);

                InvokeCommand(window, AppCommandIds.StoryRelationshipMapOpen);
                Assert.Equal(Visibility.Visible, ((FrameworkElement)window.FindName("RelationshipMapSurface")).Visibility);

                ((TextBox)window.FindName("RelationshipEntityNameBox")).Text = "윤서";
                ((TextBox)window.FindName("RelationshipEntityRoleBox")).Text = "주연";
                InvokeCommand(window, AppCommandIds.StoryAddNode);
                ((TextBox)window.FindName("RelationshipEntityNameBox")).Text = "도현";
                ((TextBox)window.FindName("RelationshipEntityRoleBox")).Text = "조력자";
                InvokeCommand(window, AppCommandIds.StoryAddNode);

                var entityList = (ListBox)window.FindName("RelationshipEntityList");
                var canvas = (Canvas)window.FindName("RelationshipMapCanvas");
                var sourceBox = (ComboBox)window.FindName("RelationshipSourceBox");
                var targetBox = (ComboBox)window.FindName("RelationshipTargetBox");

                Assert.Equal(2, entityList.Items.Count);
                Assert.Contains(canvas.Children.OfType<Border>(), node => Equals(node.Tag, "entity-0001"));
                Assert.Contains(canvas.Children.OfType<Border>(), node => Equals(node.Tag, "entity-0002"));

                sourceBox.SelectedIndex = 0;
                targetBox.SelectedIndex = 1;
                ((TextBox)window.FindName("RelationshipLabelBox")).Text = "동맹";
                InvokeCommand(window, AppCommandIds.StoryAddRelationship);

                Assert.Contains(canvas.Children.OfType<System.Windows.Shapes.Line>(), line => line.X2 > line.X1);
                Assert.Contains(canvas.Children.OfType<TextBlock>(), label => label.Text == "동맹");
                window.Close();
            }
            catch (Exception ex)
            {
                failure = ex.ToString();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            Assert.Fail(failure);
        }
    }

    [Fact]
    public void MainWindowContainsSceneInspectorSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();

                Assert.NotNull(window.FindName("InspectorPanel"));
                Assert.NotNull(window.FindName("InspectorSynopsisBox"));
                Assert.NotNull(window.FindName("InspectorStatusBox"));
                Assert.NotNull(window.FindName("InspectorTagsBox"));
                Assert.NotNull(window.FindName("InspectorTargetCountBox"));
                Assert.NotNull(window.FindName("InspectorCurrentCountText"));
                Assert.NotNull(window.FindName("InspectorContentLengthText"));
                Assert.NotNull(window.FindName("InspectorContentLengthWithSpacesText"));
                Assert.NotNull(window.FindName("InspectorSceneTypeBox"));
                Assert.NotNull(window.FindName("InspectorManualLineBreakBox"));
                Assert.NotNull(window.FindName("InspectorUpdatedAtText"));
                Assert.NotNull(window.FindName("InspectorSaveButton"));
                Assert.NotNull(window.FindName("SnapshotList"));
                Assert.NotNull(window.FindName("SnapshotCreateButton"));
                Assert.NotNull(window.FindName("SnapshotRestoreButton"));
                Assert.NotNull(window.FindName("SnapshotDeleteButton"));
                Assert.NotNull(window.FindName("SceneEntityLinkList"));
                Assert.NotNull(window.FindName("SceneEntityLinkEntityBox"));
                Assert.NotNull(window.FindName("SceneEntityLinkRoleBox"));
                Assert.NotNull(window.FindName("SceneEntityLinkNotesBox"));
                Assert.NotNull(window.FindName("SceneEntityLinkAddButton"));
                Assert.NotNull(window.FindName("SceneEntityLinkDeleteButton"));
                window.Close();
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
    public void EditingTextDoesNotRefreshPreviewAutomatically()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var editor = (TextBox)window.FindName("EditorBox");
                var preview = (TextBlock)window.FindName("PreviewText");

                preview.Text = "stale preview";
                editor.Text = new string('A', 50_000);

                Assert.Equal("stale preview", preview.Text);
                window.Close();
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
    public void MainEditorUsesLargeTextFriendlyInputSettings()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var editor = (TextBox)window.FindName("EditorBox");

                Assert.Equal(TextWrapping.NoWrap, editor.TextWrapping);
                Assert.Equal(0, editor.UndoLimit);
                Assert.False(SpellCheck.GetIsEnabled(editor));
                window.Close();
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
    public void MainEditorAcceptsLargeAppendWithinInputBudget()
    {
        Exception? failure = null;
        var elapsedMilliseconds = 0.0;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var editor = (TextBox)window.FindName("EditorBox");

                editor.Text = new string('A', 80_000);
                editor.CaretIndex = editor.Text.Length;

                var stopwatch = Stopwatch.StartNew();
                editor.SelectedText = " appended";
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

                Assert.EndsWith(" appended", editor.Text, StringComparison.Ordinal);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(250), $"Main editor large append took {stopwatch.Elapsed}.");
                window.Close();
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

        _output.WriteLine($"Main editor 80k append elapsed: {elapsedMilliseconds:N3} ms");
    }

    private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is T typed)
            {
                yield return typed;
            }

            if (child is DependencyObject dependencyObject)
            {
                foreach (var nested in FindLogicalChildren<T>(dependencyObject))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<string?> FindCommandTags(DependencyObject parent)
    {
        foreach (var tag in FindLogicalChildren<Button>(parent).Select(button => button.Tag as string))
        {
            yield return tag;
        }

        foreach (var tag in FindLogicalChildren<MenuItem>(parent).Select(item => item.Tag as string))
        {
            yield return tag;
        }
    }

    private static void InvokePrivate(MainWindow window, string methodName, params object[] args)
    {
        var method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, methodName);
        method.Invoke(window, args);
    }

    private static void InvokeCommand(MainWindow window, string commandId)
    {
        var method = typeof(MainWindow).GetMethod("ExecuteCommandAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, "ExecuteCommandAsync");
        var task = (Task)method.Invoke(window, [commandId])!;
        while (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        task.GetAwaiter().GetResult();
    }
}
