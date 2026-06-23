using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;
using System.Windows.Threading;

namespace WriterWorkbench.Tests;

public sealed class DetachedDocumentWindowTests
{
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
