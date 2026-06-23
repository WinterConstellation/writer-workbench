using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit.Abstractions;

namespace WriterWorkbench.Tests;

public sealed class DetachedDocumentWindowTests
{
    private readonly ITestOutputHelper _output;

    public DetachedDocumentWindowTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DetachedDocumentWindowConstructsOnStaThread()
    {
        await RunOnStaThreadAsync(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            var document = new WriterDocument(
                "scene-200",
                "Detached Scene",
                [
                    new WriterParagraph("p-1", "First paragraph", "Body", [], []),
                    new WriterParagraph("p-2", "Second paragraph", "Body", [], [])
                ]);

            var window = new DetachedDocumentWindow(store, document);

            Assert.Equal("분리 창 - Detached Scene", window.Title);
            Assert.Equal("Detached Scene", window.DocumentTitleText);
            Assert.Contains("First paragraph", window.BodyText);
            Assert.Contains("문단 2", window.MetricsDisplay);
            window.Close();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task DetachedDocumentWindowSavesEditedDocumentOnStaThread()
    {
        await RunOnStaThreadAsync(async () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            await store.CreateProjectAsync("Detached Test", CancellationToken.None);
            var document = new WriterDocument(
                "scene-201",
                "Before",
                [new WriterParagraph("p-1", "Before body", "Body", [], [])]);
            await store.SaveDocumentAsync(document, CancellationToken.None);

            var window = new DetachedDocumentWindow(store, document)
            {
                DocumentTitleText = "After",
                BodyText = "After body"
            };

            await window.SaveAsync(CancellationToken.None);
            var loaded = await store.LoadDocumentAsync("scene-201", CancellationToken.None);

            Assert.Equal("After", loaded.Title);
            Assert.Single(loaded.Paragraphs);
            Assert.Equal("After body", loaded.Paragraphs[0].Text);
            Assert.Contains("저장됨", window.StatusDisplay);
            window.Close();
        });
    }

    [Fact]
    public async Task DetachedEditorUsesLargeTextFriendlyInputSettings()
    {
        await RunOnStaThreadAsync(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            var document = LargeDocumentFactory.Create("scene-detached-large", "Detached Large", 15_000);
            var window = new DetachedDocumentWindow(store, document);
            var editor = (TextBox)window.FindName("DetachedEditorBox");

            Assert.Equal(TextWrapping.NoWrap, editor.TextWrapping);
            Assert.Equal(0, editor.UndoLimit);
            Assert.False(SpellCheck.GetIsEnabled(editor));
            window.Close();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task DetachedLargeDocumentAcceptsSingleAppendWithinInputBudget()
    {
        await RunOnStaThreadAsync(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            var document = LargeDocumentFactory.Create("scene-detached-large", "Detached Large", 15_000);
            var window = new DetachedDocumentWindow(store, document);
            var editor = (TextBox)window.FindName("DetachedEditorBox");

            editor.CaretIndex = editor.Text.Length;
            var stopwatch = Stopwatch.StartNew();
            editor.SelectedText = " appended";
            stopwatch.Stop();

            _output.WriteLine($"Detached 15k append elapsed: {stopwatch.Elapsed.TotalMilliseconds:N3} ms");
            Assert.EndsWith(" appended", editor.Text, StringComparison.Ordinal);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(250), $"Detached large append took {stopwatch.Elapsed}.");
            window.Close();
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task DetachedTextChangeDefersMetricsRefresh()
    {
        await RunOnStaThreadAsync(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
            var store = new ProjectStore(ProjectPaths.ForRoot(root));
            var document = LargeDocumentFactory.Create("scene-detached-large", "Detached Large", 15_000);
            var window = new DetachedDocumentWindow(store, document);
            var editor = (TextBox)window.FindName("DetachedEditorBox");
            var metricsBeforeEdit = window.MetricsDisplay;

            editor.CaretIndex = editor.Text.Length;
            editor.SelectedText = " appended";

            Assert.Equal(metricsBeforeEdit, window.MetricsDisplay);
            window.Close();
            return Task.CompletedTask;
        });
    }

    private static Task RunOnStaThreadAsync(Func<Task> action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await action();
                    completion.SetResult();
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            });

            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
