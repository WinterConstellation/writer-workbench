using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WriterWorkbench.Core.Commands;
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
                var commandTags = FindLogicalChildren<Button>(window)
                    .Select(button => button.Tag as string)
                    .Where(tag => tag is not null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Assert.Contains(AppCommandIds.WorkspacePresetOne, commandTags);
                Assert.Contains(AppCommandIds.WorkspacePresetTwo, commandTags);
                Assert.Contains(AppCommandIds.WorkspacePresetThree, commandTags);
                Assert.Contains(AppCommandIds.WorkspaceStartupPresetCycle, commandTags);
                Assert.Contains(AppCommandIds.ShortcutsOpenSettings, commandTags);
                Assert.Contains(AppCommandIds.ViewMainOpen, commandTags);
                Assert.Contains(AppCommandIds.ViewPreviewToggle, commandTags);
                Assert.Contains(AppCommandIds.HelpOpen, commandTags);
                Assert.Contains(AppCommandIds.ExportCurrentScene, commandTags);
                Assert.Contains(AppCommandIds.ExportFullManuscript, commandTags);
                Assert.Contains(AppCommandIds.SnapshotCreateCurrent, commandTags);
                Assert.Contains(AppCommandIds.SnapshotRestoreSelected, commandTags);
                Assert.Contains(AppCommandIds.SnapshotDeleteSelected, commandTags);
                Assert.Contains(AppCommandIds.StoryAddNode, commandTags);
                Assert.Contains(AppCommandIds.StoryAddRelationship, commandTags);
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
    public void MainWindowContainsLongOperationProgressSurface()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new MainWindow();

                Assert.NotNull(window.FindName("EditorSurface"));
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
}
