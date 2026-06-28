using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;
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
        string? failure = null;
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
                Assert.DoesNotContain(AppCommandIds.ViewHtmlWorkbenchOpen, commandTags);
                Assert.Contains(AppCommandIds.ViewEditorOpen, commandTags);
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
    public void MainWindowShowsRemoteControlLayerDockedToRightOfOwnerByDefault()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow
                {
                    Left = 120,
                    Top = 80,
                    Width = 900
                };
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                InvokePrivate(window, "ShowRemoteControlLayer", true);

                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                Assert.Equal(1030d, layer.Left);
                Assert.Equal(176d, layer.Top);
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

                Assert.NotNull(window.FindName("NativeCommandChrome"));
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
    public void MainWindowInitializationCreatesSeparatedProjectSettingsFiles()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"), "Sample.writerproj");
                var paths = ProjectPaths.ForRoot(root);
                var window = new MainWindow();

                InvokePrivate(window, "ConfigureProject", root);
                typeof(MainWindow)
                    .GetField("_startupStateLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, true);
                WaitForTaskOnDispatcher((Task)typeof(MainWindow)
                    .GetMethod("InitializeProjectAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, [])!);

                Assert.True(File.Exists(paths.AppSettingsPath), paths.AppSettingsPath);
                Assert.True(File.Exists(paths.WidgetRegistryPath), paths.WidgetRegistryPath);
                Assert.Contains("마지막 작업", File.ReadAllText(paths.AppSettingsPath));
                Assert.Contains("widget-registry", File.ReadAllText(paths.WidgetRegistryPath));
                Assert.Equal(
                    Visibility.Visible,
                    Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface")).Visibility);
                Assert.Contains("html-workbench", File.ReadAllText(paths.AppSettingsPath));
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
                var htmlSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface"));
                var editorSurface = Assert.IsType<DockPanel>(window.FindName("EditorSurface"));
                var nativeChrome = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("NativeCommandChrome"));
                var statusText = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("StatusText"));
                var metricsText = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("MetricsText"));

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");

                Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
                Assert.Equal(3, Grid.GetColumnSpan(htmlSurface));
                Assert.Equal(Visibility.Collapsed, editorSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, nativeChrome.Visibility);
                Assert.Equal(Visibility.Collapsed, statusText.Visibility);
                Assert.Equal(Visibility.Collapsed, metricsText.Visibility);

                InvokePrivate(window, "ShowMainSurface");

                Assert.Equal(Visibility.Collapsed, htmlSurface.Visibility);
                Assert.Equal(Visibility.Visible, nativeChrome.Visibility);
                Assert.Equal(Visibility.Visible, statusText.Visibility);
                Assert.Equal(Visibility.Visible, metricsText.Visibility);
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
    public void MainWindowMainCommandOpensHtmlMainSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var htmlSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface"));
                var mainSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("MainSurface"));
                var editorSurface = Assert.IsType<DockPanel>(window.FindName("EditorSurface"));
                var nativeChrome = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("NativeCommandChrome"));

                InvokeCommand(window, AppCommandIds.ViewMainOpen);

                Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, mainSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, editorSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, nativeChrome.Visibility);
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
    public void MainWindowRestoresLegacyMainSurfaceAsHtmlMainSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var htmlSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface"));
                var mainSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("MainSurface"));
                var nativeChrome = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("NativeCommandChrome"));

                InvokePrivateAsync(window, "ShowRequestedSurfaceAsync", AppSessionState.MainSurface);

                Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, mainSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, nativeChrome.Visibility);
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
    public void MainWindowEditorCommandStaysInsideHtmlMainSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var htmlSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface"));
                var editorSurface = Assert.IsType<DockPanel>(window.FindName("EditorSurface"));
                var nativeChrome = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("NativeCommandChrome"));
                var statusText = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("StatusText"));
                var metricsText = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("MetricsText"));

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");
                InvokeCommand(window, AppCommandIds.ViewEditorOpen);

                Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, editorSurface.Visibility);
                Assert.Equal(Visibility.Collapsed, nativeChrome.Visibility);
                Assert.Equal(Visibility.Collapsed, statusText.Visibility);
                Assert.Equal(Visibility.Collapsed, metricsText.Visibility);
                Assert.Equal("editor", GetPrivateField<string>(window, "_htmlActiveView"));
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
    public void MainWindowHtmlEditorPayloadLoadsFirstDocumentWhenActiveDocumentIsMissing()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");
                var manifest = WaitForTaskOnDispatcher(store.CreateProjectAsync("원고 화면", CancellationToken.None));
                InvokePrivateAsync(window, "RefreshBinderAsync", manifest);
                typeof(MainWindow).GetField("_activeDocument", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, null);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, "");

                var payload = InvokePrivateAsync<WriterWorkbench.Core.WebWorkbench.WebWorkbenchPayload>(
                    window,
                    "CreateHtmlWorkbenchPayloadAsync",
                    "editor");

                Assert.NotNull(payload.ActiveScene);
                Assert.Equal("scene-0001", payload.ActiveScene!.Id);
                Assert.Contains("여기에 원고를 씁니다.", payload.ActiveScene.EditorText);
                Assert.NotNull(GetPrivateField<WriterDocument>(window, "_activeDocument"));
                Assert.False(GetPrivateField<bool>(window, "_dirty"));
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
    public void MainWindowViewCommandsStayInsideHtmlShellBoundary()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var htmlSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("HtmlWorkbenchSurface"));
                var editorSurface = Assert.IsType<DockPanel>(window.FindName("EditorSurface"));
                var previewSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("PreviewSurface"));
                var relationshipSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("RelationshipMapSurface"));
                var nativeChrome = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("NativeCommandChrome"));

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");

                InvokeCommand(window, AppCommandIds.ViewPreviewToggle);
                AssertHtmlOnlySurface(window, htmlSurface, editorSurface, previewSurface, relationshipSurface, nativeChrome);
                Assert.Equal("preview", GetPrivateField<string>(window, "_htmlActiveView"));

                InvokeCommand(window, AppCommandIds.StoryRelationshipMapOpen);
                AssertHtmlOnlySurface(window, htmlSurface, editorSurface, previewSurface, relationshipSurface, nativeChrome);
                Assert.Equal("relationship-map", GetPrivateField<string>(window, "_htmlActiveView"));

                InvokeCommand(window, AppCommandIds.HelpOpen);
                AssertHtmlOnlySurface(window, htmlSurface, editorSurface, previewSurface, relationshipSurface, nativeChrome);
                Assert.Equal("help", GetPrivateField<string>(window, "_htmlActiveView"));

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
    public void MainWindowHtmlWorkbenchKeepsNativeRemoteControlLayerAvailable()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(layerField);
                InvokePrivate(window, "ShowHtmlWorkbenchSurface");
                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                Assert.True(layer.IsVisible);
                Assert.True(layer.Topmost);

                InvokeCommand(window, AppCommandIds.RemoteControlToggle);
                Assert.False(layer.IsVisible);

                InvokeCommand(window, AppCommandIds.RemoteControlToggle);
                Assert.True(layer.IsVisible);
                Assert.True(layer.Topmost);

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
    public void MainWindowHtmlWorkbenchShowsNativeRemoteControlLayerWhenEntering()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(layerField);
                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                layer.Hide();

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");

                Assert.True(layer.IsVisible);
                Assert.True(layer.Topmost);
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
    public void MainWindowShowingHtmlWorkbenchDoesNotHideVisibleNativeRemoteControlLayer()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var layerField = typeof(MainWindow).GetField(
                    "_remoteControlLayer",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(layerField);
                InvokePrivate(window, "ShowRemoteControlLayer", true);
                var layer = Assert.IsType<RemoteControlLayerWindow>(layerField!.GetValue(window));
                Assert.True(layer.IsVisible);

                InvokePrivate(window, "ShowHtmlWorkbenchSurface");

                Assert.True(layer.IsVisible);
                Assert.True(layer.Topmost);
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
    public void MainWindowHtmlBinderCommandSelectsSceneBeforeRunningBinderCommand()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");

                WaitForTaskOnDispatcher(store.CreateProjectAsync("HTML 바인더", CancellationToken.None));
                var second = WaitForTaskOnDispatcher(store.CreateDocumentAsync("두 번째", CancellationToken.None));
                var manifest = WaitForTaskOnDispatcher(store.LoadManifestAsync(CancellationToken.None));
                InvokePrivateAsync(window, "RefreshBinderAsync", manifest);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, "scene-0001");

                InvokePrivateAsync(window, "ApplyHtmlBinderCommandAsync", second.Id, AppCommandIds.DocumentMoveSceneUp);

                var reordered = WaitForTaskOnDispatcher(store.LoadManifestAsync(CancellationToken.None));
                var selectedItem = Assert.IsAssignableFrom<object>(Assert.IsType<ListBox>(window.FindName("BinderList")).SelectedItem);
                var selectedId = Assert.IsType<string>(selectedItem.GetType().GetProperty("Id")!.GetValue(selectedItem));
                Assert.Equal(second.Id, reordered.Documents.First().Id);
                Assert.Equal(second.Id, GetPrivateField<string>(window, "_activeDocumentId"));
                Assert.Equal(second.Id, selectedId);
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
    public void MainWindowHtmlBinderReorderPersistsManifestOrder()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");

                var first = WaitForTaskOnDispatcher(store.CreateDocumentAsync("첫 장면", CancellationToken.None));
                var second = WaitForTaskOnDispatcher(store.CreateDocumentAsync("두 번째", CancellationToken.None));
                var third = WaitForTaskOnDispatcher(store.CreateDocumentAsync("세 번째", CancellationToken.None));
                var manifest = WaitForTaskOnDispatcher(store.LoadManifestAsync(CancellationToken.None));
                InvokePrivateAsync(window, "RefreshBinderAsync", manifest);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, second.Id);

                InvokePrivateAsync(window, "ApplyHtmlBinderReorderAsync", (object)new[] { third.Id, first.Id, second.Id });

                var reordered = WaitForTaskOnDispatcher(store.LoadManifestAsync(CancellationToken.None));
                var selectedItem = Assert.IsAssignableFrom<object>(Assert.IsType<ListBox>(window.FindName("BinderList")).SelectedItem);
                var selectedId = Assert.IsType<string>(selectedItem.GetType().GetProperty("Id")!.GetValue(selectedItem));

                Assert.Equal([third.Id, first.Id, second.Id], reordered.Documents.Select(document => document.Id));
                Assert.Equal(second.Id, GetPrivateField<string>(window, "_activeDocumentId"));
                Assert.Equal(second.Id, selectedId);
                Assert.Contains("바인더 순서 저장됨", GetStatusText(window));
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
    public void MainWindowHtmlSceneMetadataUpdatePersistsStatusAndCategory()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");
                var metadataStore = GetPrivateField<SceneMetadataStore>(window, "_metadataStore");

                var document = WaitForTaskOnDispatcher(store.CreateDocumentAsync("상태 수정 장면", CancellationToken.None));
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, document.Id);

                InvokePrivateAsync(window, "ApplyHtmlSceneMetadataUpdateAsync", document.Id, "업로드대기", "자료");

                var saved = WaitForTaskOnDispatcher(metadataStore.LoadAsync(document.Id, CancellationToken.None));
                var inspectorStatus = Assert.IsType<ComboBox>(window.FindName("InspectorStatusBox"));
                var inspectorCategory = Assert.IsType<TextBox>(window.FindName("InspectorFileCategoryBox"));

                Assert.Equal(SceneStatus.UploadPending, saved.Status);
                Assert.Equal("자료", saved.FileCategory);
                Assert.Equal(SceneStatus.UploadPending, inspectorStatus.SelectedValue);
                Assert.Equal("자료", inspectorCategory.Text);
                Assert.Contains($"장면 정보 저장됨 {document.Id}", GetStatusText(window));
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
    public void MainWindowHtmlSceneMemoUpdatePersistsWithoutMutatingBody()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");
                var metadataStore = GetPrivateField<SceneMetadataStore>(window, "_metadataStore");

                var document = new WriterDocument(
                    "scene-memo",
                    "메모 장면",
                    [new WriterParagraph("p-0001", "본문은 유지", "body", [], [])]);
                WaitForTaskOnDispatcher(store.SaveDocumentAsync(document, CancellationToken.None));
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(window, document.Id);

                InvokePrivateAsync(window, "ApplyHtmlSceneMemoUpdateAsync", document.Id, "작가 메모");

                var saved = WaitForTaskOnDispatcher(metadataStore.LoadAsync(document.Id, CancellationToken.None));
                var loadedDocument = WaitForTaskOnDispatcher(store.LoadDocumentAsync(document.Id, CancellationToken.None));

                Assert.Equal("작가 메모", saved.Memo);
                Assert.Equal("본문은 유지", loadedDocument.Paragraphs[0].Text);
                Assert.Contains($"장면 메모 저장됨 {document.Id}", GetStatusText(window));
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
    public void MainWindowAppliesHtmlRemoteSettingsUpdateToCustomizationProfile()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);

                InvokePrivateAsync(
                    window,
                    "ApplyHtmlRemoteSettingsUpdateAsync",
                    (object)new[] { AppCommandIds.HelpOpen, AppCommandIds.ProjectSave });

                var profile = GetPrivateField<WorkbenchCustomizationProfile>(window, "_activeCustomizationProfile");
                var remote = new WorkbenchCustomizationResolver(profile)
                    .GetPlacements("remote", "main")
                    .ToList();

                Assert.Equal([AppCommandIds.HelpOpen, AppCommandIds.ProjectSave], remote.Select(placement => placement.CommandId));
                Assert.Equal(["remote-01", "remote-02"], remote.Select(placement => placement.SlotKey));
                Assert.Equal("리모컨 바로가기 2개 저장됨", GetStatusText(window));
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
    public void MainWindowAppliesHtmlActiveSceneEditorUpdateToCurrentDocument()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var original = new WriterDocument(
                    "scene-html",
                    "HTML 장면",
                    [new WriterParagraph("p-0001", "처음 본문", "body", [], [])]);
                var editor = Assert.IsType<TextBox>(window.FindName("EditorBox"));
                var title = Assert.IsType<TextBox>(window.FindName("TitleBox"));

                typeof(MainWindow).GetField("_activeDocument", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, original);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, original.Id);
                typeof(MainWindow).GetField("_editorTextView", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
                    window,
                    DocumentEditorTextService.CreateView(original));
                title.Text = original.Title;
                editor.Text = "처음 본문";

                InvokePrivate(window, "ApplyHtmlActiveSceneUpdate", "HTML 수정", "바뀐 본문\n\n둘째 문단");

                var updated = Assert.IsType<WriterDocument>(
                    typeof(MainWindow).GetField("_activeDocument", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window));
                var dirty = Assert.IsType<bool>(
                    typeof(MainWindow).GetField("_dirty", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window));

                Assert.True(dirty);
                Assert.Equal("HTML 수정", updated.Title);
                Assert.Equal(["바뀐 본문", "둘째 문단"], updated.Paragraphs.Select(paragraph => paragraph.Text));
                Assert.Equal("HTML 수정", title.Text);
                Assert.Equal("바뀐 본문\n\n둘째 문단", editor.Text.Replace("\r\n", "\n", StringComparison.Ordinal));
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
    public void MainWindowHtmlEditorViewClaimsEditorSurfaceForDetachedDuplicateGuard()
    {
        string? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var registry = GetPrivateField<WorkbenchSurfaceClaimRegistry>(window, "_surfaceClaims");

                InvokeCommand(window, AppCommandIds.ViewEditorOpen);
                var detached = new WorkbenchDetachedWindow(registry, "detached-editor-claim-test");
                var editorButton = Assert.IsType<Button>(detached.FindName("DetachedEditorSurfaceButton"));
                var previewButton = Assert.IsType<Button>(detached.FindName("DetachedPreviewSurfaceButton"));

                Assert.True(registry.IsClaimedBy(WorkbenchSurfaceClaimRegistry.MainOwnerId, AppSessionState.EditorSurface));
                Assert.False(editorButton.IsEnabled);
                Assert.True(previewButton.IsEnabled);

                detached.Close();
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
    public void MainWindowDoesNotSwitchHtmlViewWhenDetachedSurfaceOwnsTarget()
    {
        string? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var registry = GetPrivateField<WorkbenchSurfaceClaimRegistry>(window, "_surfaceClaims");
                var detached = new WorkbenchDetachedWindow(registry, "detached-preview-owner-test");

                Assert.True(detached.TrySelectSurface(AppSessionState.PreviewSurface));
                InvokeCommand(window, AppCommandIds.ViewPreviewToggle);

                Assert.True(registry.IsClaimedBy("detached-preview-owner-test", AppSessionState.PreviewSurface));
                Assert.Equal("editor", GetPrivateField<string>(window, "_htmlActiveView"));
                Assert.Contains("이미 다른 분리 작업대", GetStatusText(window));

                detached.Close();
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
    public void MainWindowCapturesDetachedWorkbenchWindowsInPreset()
    {
        string? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var registry = GetPrivateField<WorkbenchSurfaceClaimRegistry>(window, "_surfaceClaims");
                var detachedWindows = GetPrivateField<List<WorkbenchDetachedWindow>>(window, "_detachedWorkbenchWindows");
                var detached = new WorkbenchDetachedWindow(registry, "detached-preset-capture-test")
                {
                    Left = 1700,
                    Top = 40,
                    Width = 620,
                    Height = 720
                };
                Assert.True(detached.TrySelectSurface(AppSessionState.PreviewSurface));
                detachedWindows.Add(detached);

                var preset = InvokePrivate<WorkspacePreset>(window, "CapturePreset", 1);

                var detachedPreset = Assert.Single(preset.DetachedWindows ?? []);
                Assert.Equal(AppSessionState.PreviewSurface, detachedPreset.SurfaceId);
                Assert.Equal(1700, detachedPreset.Placement.Left);
                Assert.Equal(620, detachedPreset.Placement.Width);

                detached.Close();
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
    public void MainWindowAppliesDetachedWorkbenchWindowsFromPreset()
    {
        string? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var preset = new WorkspacePreset(
                    1,
                    "Detached desk",
                    MonitorRegion.Full,
                    false,
                    new WindowPlacement(10, 20, 1200, 800, "Normal"),
                    [
                        new WorkspaceDetachedWindowPlacement(
                            AppSessionState.PreviewSurface,
                            new WindowPlacement(1800, 30, 640, 720, "Normal")),
                        new WorkspaceDetachedWindowPlacement(
                            AppSessionState.RelationshipMapSurface,
                            new WindowPlacement(2460, 30, 640, 720, "Normal"))
                    ]);

                InvokePrivate(window, "ApplyPreset", preset);

                var detachedWindows = GetPrivateField<List<WorkbenchDetachedWindow>>(window, "_detachedWorkbenchWindows");
                Assert.Equal(2, detachedWindows.Count);
                Assert.Equal(AppSessionState.PreviewSurface, detachedWindows[0].AssignedSurfaceId);
                Assert.Equal(1800, detachedWindows[0].Left);
                Assert.Equal(640, detachedWindows[0].Width);
                Assert.Equal(AppSessionState.RelationshipMapSurface, detachedWindows[1].AssignedSurfaceId);

                foreach (var detached in detachedWindows.ToList())
                {
                    detached.Close();
                }

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
                Assert.Equal(Visibility.Visible, ((FrameworkElement)window.FindName("HtmlWorkbenchSurface")).Visibility);
                Assert.Equal(Visibility.Collapsed, ((FrameworkElement)window.FindName("RelationshipMapSurface")).Visibility);
                Assert.Equal("relationship-map", GetPrivateField<string>(window, "_htmlActiveView"));

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
    public void MainWindowHtmlStoryCommandsPersistRelationshipMapData()
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
                var store = GetPrivateField<StoryStructureStore>(window, "_storyStructureStore");

                InvokePrivateAsync(window, "ApplyHtmlStoryEntityAddAsync", "Character", "윤서", "주연");
                InvokePrivateAsync(window, "ApplyHtmlStoryEntityAddAsync", "Character", "도현", "조력자");
                InvokePrivateAsync(window, "ApplyHtmlStoryRelationshipAddAsync", "entity-0001", "entity-0002", "동맹", "");
                InvokePrivateAsync(window, "ApplyHtmlStoryLayoutUpdateAsync", "entity-0002", 333d, 144d);

                var entities = WaitForTaskOnDispatcher(store.LoadEntitiesAsync(CancellationToken.None));
                var relationships = WaitForTaskOnDispatcher(store.LoadRelationshipsAsync(CancellationToken.None));
                var layout = WaitForTaskOnDispatcher(store.LoadRelationLayoutAsync(CancellationToken.None));

                Assert.Equal(["윤서", "도현"], entities.Select(entity => entity.Name));
                Assert.Equal("동맹", Assert.Single(relationships).Label);
                var node = Assert.Single(layout, item => item.EntityId == "entity-0002");
                Assert.Equal(333d, node.X);
                Assert.Equal(144d, node.Y);
                Assert.Contains("관계도 위치 저장됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlStoryCommandsUpdateAndDeleteRelationshipMapData()
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
                var store = GetPrivateField<StoryStructureStore>(window, "_storyStructureStore");

                InvokePrivateAsync(window, "ApplyHtmlStoryEntityAddAsync", "Character", "윤서", "주연");
                InvokePrivateAsync(window, "ApplyHtmlStoryEntityAddAsync", "Character", "도현", "조력자");
                InvokePrivateAsync(window, "ApplyHtmlStoryEntityUpdateAsync", "entity-0001", "Concept", "윤서 수정", "화자");
                InvokePrivateAsync(window, "ApplyHtmlStoryRelationshipAddAsync", "entity-0001", "entity-0002", "동맹", "서로 돕는다");
                InvokePrivateAsync(window, "ApplyHtmlStoryRelationshipUpdateAsync", "rel-0001", "entity-0001", "entity-0002", "긴장", "믿지 못한다");
                InvokePrivateAsync(window, "ApplyHtmlStoryRelationshipDeleteAsync", "rel-0001");
                InvokePrivateAsync(window, "ApplyHtmlStoryEntityDeleteAsync", "entity-0002");

                var entities = WaitForTaskOnDispatcher(store.LoadEntitiesAsync(CancellationToken.None));
                var relationships = WaitForTaskOnDispatcher(store.LoadRelationshipsAsync(CancellationToken.None));

                Assert.Equal("윤서 수정", Assert.Single(entities).Name);
                Assert.Equal("화자", Assert.Single(entities).Role);
                Assert.Equal(StoryEntityType.Concept, Assert.Single(entities).Type);
                Assert.Empty(relationships);
                Assert.Contains("관계도 노드 삭제됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlSettingsBookCommandsPersistItems()
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
                var store = GetPrivateField<StoryStructureStore>(window, "_storyStructureStore");

                InvokePrivateAsync(window, "ApplyHtmlSettingsBookAddAsync", "World", "동부 왕국", "비가 자주 오는 국경 도시", new[] { "세계관", "도시" });

                var syncedEntities = WaitForTaskOnDispatcher(store.LoadEntitiesAsync(CancellationToken.None));
                var syncedEntity = Assert.Single(syncedEntities);
                Assert.Equal("settings-note-0001", syncedEntity.Id);
                Assert.Equal(StoryEntityType.Concept, syncedEntity.Type);
                Assert.Equal("동부 왕국", syncedEntity.Name);
                Assert.Equal("세계관", syncedEntity.Role);

                InvokePrivateAsync(window, "ApplyHtmlSettingsBookUpdateAsync", "note-0001", "Reference", "동부 왕국 자료", "무역로와 항구 기록", new[] { "자료" });

                var items = WaitForTaskOnDispatcher(store.LoadSettingsBookAsync(CancellationToken.None));

                var item = Assert.Single(items);
                Assert.Equal("note-0001", item.Id);
                Assert.Equal(StorySettingsBookCategory.Reference, item.Category);
                Assert.Equal("동부 왕국 자료", item.Title);
                Assert.Equal("무역로와 항구 기록", item.Body);
                Assert.Empty(WaitForTaskOnDispatcher(store.LoadEntitiesAsync(CancellationToken.None)));
                Assert.True(File.Exists(ProjectPaths.ForRoot(root).StorySettingsBookPath));
                Assert.Contains("설정집 항목 수정됨", ((TextBlock)window.FindName("StatusText")).Text);

                InvokePrivateAsync(window, "ApplyHtmlSettingsBookDeleteAsync", "note-0001");

                Assert.Empty(WaitForTaskOnDispatcher(store.LoadSettingsBookAsync(CancellationToken.None)));
                Assert.Contains("설정집 항목 삭제됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlSettingsBookSynopsisSyncsToWhiteboardEntity()
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
                var store = GetPrivateField<StoryStructureStore>(window, "_storyStructureStore");

                InvokePrivateAsync(window, "ApplyHtmlSettingsBookAddAsync", "Synopsis", "1부 시놉시스", "발단과 전환점 정리", new[] { "시놉시스", "1부" });

                var item = Assert.Single(WaitForTaskOnDispatcher(store.LoadSettingsBookAsync(CancellationToken.None)));
                var entity = Assert.Single(WaitForTaskOnDispatcher(store.LoadEntitiesAsync(CancellationToken.None)));

                Assert.Equal(StorySettingsBookCategory.Synopsis, item.Category);
                Assert.Equal("settings-note-0001", entity.Id);
                Assert.Equal(StoryEntityType.Event, entity.Type);
                Assert.Equal("시놉시스", entity.Role);
                Assert.Equal("1부 시놉시스", entity.Name);
                Assert.Contains("설정집 항목 추가됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlTextReplacementAppliesToActiveSceneOnlyWhenRequested()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var original = new WriterDocument(
                    "scene-replace",
                    "대치 장면",
                    [new WriterParagraph("p-0001", "첫 문장... 안되...", "body", [], [])]);
                typeof(MainWindow).GetField("_activeDocument", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, original);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, original.Id);
                typeof(MainWindow).GetField("_editorTextView", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
                    window,
                    DocumentEditorTextService.CreateView(original));

                InvokePrivateAsync(window, "ApplyHtmlTextReplacementAddAsync", "...", "…");
                InvokePrivateAsync(window, "ApplyHtmlTextReplacementAddAsync", "안되", "안 돼");
                var unchanged = GetPrivateField<WriterDocument>(window, "_activeDocument");

                InvokePrivateAsync(window, "ApplyHtmlTextReplacementsToActiveSceneAsync");
                var updated = GetPrivateField<WriterDocument>(window, "_activeDocument");

                Assert.Equal("첫 문장... 안되...", Assert.Single(unchanged.Paragraphs).Text);
                Assert.Equal("첫 문장… 안 돼…", Assert.Single(updated.Paragraphs).Text);
                Assert.True(GetPrivateField<bool>(window, "_dirty"));
                Assert.Contains("대치어 적용됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlWordAnalysisAggregatesSelectedDocumentsWithActiveEditorText()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var window = new MainWindow();
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
                InvokePrivate(window, "ConfigureProject", root);
                var store = GetPrivateField<ProjectStore>(window, "_store");
                var first = new WriterDocument(
                    "scene-0001",
                    "첫 장면",
                    [new WriterParagraph("p-0001", "검은 초안", "body", [], [])]);
                var second = new WriterDocument(
                    "scene-0002",
                    "둘째 장면",
                    [new WriterParagraph("p-0001", "검은 다른", "body", [], [])]);
                WaitForTaskOnDispatcher(store.SaveDocumentAsync(first, CancellationToken.None));
                WaitForTaskOnDispatcher(store.SaveDocumentAsync(second, CancellationToken.None));
                typeof(MainWindow).GetField("_activeDocument", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, first);
                typeof(MainWindow).GetField("_activeDocumentId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, first.Id);
                typeof(MainWindow).GetField("_editorTextView", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
                    window,
                    DocumentEditorTextService.CreateView(first));

                InvokePrivateAsync(
                    window,
                    "ApplyHtmlWordAnalysisAsync",
                    "selected",
                    new[] { first.Id, second.Id },
                    "첫 장면",
                    "검은 검은 수정");
                var payload = InvokePrivateAsync<WriterWorkbench.Core.WebWorkbench.WebWorkbenchPayload>(
                    window,
                    "CreateHtmlWorkbenchPayloadAsync",
                    "editor");

                Assert.NotNull(payload.WordAnalysis);
                Assert.Equal("selected", payload.WordAnalysis!.Scope);
                Assert.Equal(2, payload.WordAnalysis.DocumentCount);
                Assert.Contains(payload.WordAnalysis.Entries, entry => entry.Word == "검은" && entry.Count == 3);
                Assert.Contains("반복어 분석됨", ((TextBlock)window.FindName("StatusText")).Text);
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
    public void MainWindowHtmlShortcutCommandPersistsBinding()
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

                InvokePrivateAsync(window, "ApplyHtmlShortcutUpdateAsync", AppCommandIds.HelpOpen, "Workbench", "Ctrl+Alt+H");

                var shortcuts = GetPrivateField<ShortcutManager>(window, "_shortcutManager");
                Assert.Equal(AppCommandIds.HelpOpen, shortcuts.FindCommand("Ctrl+Alt+H", CommandScope.Workbench));
                Assert.True(File.Exists(ProjectPaths.ForRoot(root).ShortcutsPath));
                Assert.Contains("단축키 저장됨", ((TextBlock)window.FindName("StatusText")).Text);
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
                var statusBox = Assert.IsType<ComboBox>(window.FindName("InspectorStatusBox"));
                Assert.NotNull(window.FindName("InspectorFileCategoryBox"));
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
                var labels = statusBox.Items
                    .Cast<object>()
                    .Select(item => item.GetType().GetProperty("Label")!.GetValue(item)?.ToString())
                    .ToList();
                Assert.Equal(["초고", "퇴고중", "퇴고완료", "업로드대기", "업로드완료", "제외"], labels);
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
    public void MainWindowUsesExplicitCharacterCountLabels()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();
                var labels = FindLogicalChildren<TextBlock>(window)
                    .Select(textBlock => textBlock.Text)
                    .ToList();

                Assert.Contains("현재 문단", labels);
                Assert.Contains("전체 공백 제외", labels);
                Assert.Contains("전체 공백 포함", labels);
                Assert.Contains("파일 분류", labels);
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
    public void Stress15kCommandCreatesFifteenThousandCharacterDocument()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"), "Stress15k.writerproj");
                var paths = ProjectPaths.ForRoot(root);
                var window = new MainWindow();
                InvokePrivate(window, "ConfigureProject", root);

                InvokeCommand(window, AppCommandIds.DocumentCreateStressLarge);

                var manifest = WaitForTaskOnDispatcher(new ProjectStore(paths).LoadManifestAsync(CancellationToken.None));
                var stressInfo = Assert.Single(manifest.Documents.Where(document => document.Id.StartsWith("stress-", StringComparison.Ordinal)));
                var document = WaitForTaskOnDispatcher(new ProjectStore(paths).LoadDocumentAsync(stressInfo.Id, CancellationToken.None));
                var metadata = WaitForTaskOnDispatcher(new SceneMetadataStore(paths).LoadAsync(stressInfo.Id, CancellationToken.None));
                var metrics = DocumentMetricsService.Measure(document);

                Assert.Equal("스트레스 15k", document.Title);
                Assert.Equal(15_000, metrics.CharacterCount);
                Assert.Equal(15_000, metadata.ContentLengthWithSpaces);
                Assert.True(metadata.ContentLength <= metadata.ContentLengthWithSpaces);
                Assert.InRange(document.Paragraphs.Count, 1, 200);
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

    private static T InvokePrivate<T>(MainWindow window, string methodName, params object[] args)
    {
        var method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, methodName);
        return Assert.IsType<T>(method.Invoke(window, args));
    }

    private static void InvokePrivateAsync(MainWindow window, string methodName, params object[] args)
    {
        var method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, methodName);
        var task = (Task)method.Invoke(window, args)!;
        WaitForTaskOnDispatcher(task);
        task.GetAwaiter().GetResult();
    }

    private static T InvokePrivateAsync<T>(MainWindow window, string methodName, params object[] args)
    {
        var method = typeof(MainWindow).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(MainWindow).FullName, methodName);
        var task = Assert.IsAssignableFrom<Task<T>>(method.Invoke(window, args));
        return WaitForTaskOnDispatcher(task);
    }

    private static T GetPrivateField<T>(MainWindow window, string fieldName)
    {
        var field = typeof(MainWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(MainWindow).FullName, fieldName);
        return Assert.IsType<T>(field.GetValue(window));
    }

    private static string GetStatusText(MainWindow window)
    {
        return Assert.IsType<TextBlock>(window.FindName("StatusText")).Text;
    }

    private static void AssertHtmlOnlySurface(
        MainWindow window,
        FrameworkElement htmlSurface,
        FrameworkElement editorSurface,
        FrameworkElement previewSurface,
        FrameworkElement relationshipSurface,
        FrameworkElement nativeChrome)
    {
        var mainSurface = Assert.IsAssignableFrom<FrameworkElement>(window.FindName("MainSurface"));
        Assert.Equal(Visibility.Visible, htmlSurface.Visibility);
        Assert.Equal(Visibility.Collapsed, editorSurface.Visibility);
        Assert.Equal(Visibility.Collapsed, previewSurface.Visibility);
        Assert.Equal(Visibility.Collapsed, relationshipSurface.Visibility);
        Assert.Equal(Visibility.Collapsed, mainSurface.Visibility);
        Assert.Equal(Visibility.Collapsed, nativeChrome.Visibility);
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

    private static void WaitForTaskOnDispatcher(Task task)
    {
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

    private static T WaitForTaskOnDispatcher<T>(Task<T> task)
    {
        WaitForTaskOnDispatcher((Task)task);
        return task.GetAwaiter().GetResult();
    }
}
