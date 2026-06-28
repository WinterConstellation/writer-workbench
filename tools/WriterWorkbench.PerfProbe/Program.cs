using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WriterWorkbench;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.PerfProbe;

internal static class Program
{
    private const int ParagraphCount = 15_000;
    private static readonly Size ProbeWindowSize = new(1280, 800);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [STAThread]
    private static int Main()
    {
        var metrics = new List<ProbeMetric>();
        var root = Path.Combine(
            Path.GetTempPath(),
            "WriterWorkbenchPerfProbe",
            DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);

        var document = Measure(
            metrics,
            "1. stress 15k paragraphs 생성",
            "LargeDocumentFactory.Create",
            () => LargeDocumentFactory.Create("stress-15000-paragraphs", "Stress 15k paragraphs", ParagraphCount),
            $"{ParagraphCount:N0} paragraphs");

        var fullText = Measure(
            metrics,
            "5. 저장 하위 비용",
            "TXT export",
            () => TextExportService.ToPlainText(document),
            "plain manuscript text");

        var editorView = Measure(
            metrics,
            "3. EditorBox 텍스트 주입",
            "Create editor segment",
            () => DocumentEditorTextService.CreateView(document),
            "current app path: bounded front segment");

        var json = Measure(
            metrics,
            "5. 저장 하위 비용",
            "JSON serialize",
            () => JsonSerializer.Serialize(document, JsonOptions),
            "wwdoc.json payload");

        MeasureTask(
            metrics,
            "5. 저장 하위 비용",
            "JSON atomic write",
            () => WriteUtf8AtomicAsync(paths.DocumentJsonPath("json-component"), json),
            "same flush-to-disk style as ProjectStore");

        MeasureTask(
            metrics,
            "5. 저장 하위 비용",
            "TXT atomic write",
            () => WriteUtf8AtomicAsync(paths.DocumentTextPath("txt-component"), fullText),
            "same flush-to-disk style as ProjectStore");

        MeasureTask(
            metrics,
            "5. 저장 하위 비용",
            "SQLite upsert",
            () => new SqliteProjectIndex(paths.ProjectDatabasePath)
                .UpsertDocumentAsync(document, fullText, DateTimeOffset.UtcNow, CancellationToken.None),
            "single document index update");

        MeasureTask(
            metrics,
            "5. 저장 시 JSON/TXT/SQLite 비용",
            "ProjectStore.SaveDocumentAsync total",
            () => store.SaveDocumentAsync(document, CancellationToken.None),
            "JSON + TXT + manifest + SQLite");

        var loaded = MeasureTask(
            metrics,
            "2. 문서 로드 비용",
            "ProjectStore.LoadDocumentAsync",
            () => store.LoadDocumentAsync(document.Id, CancellationToken.None),
            "load wwdoc.json and repair guard");

        using (var scope = new MainWindowProbeScope())
        {
            var commandRoot = Path.Combine(root, "stress-command.writerproj");
            scope.InvokePrivate("ConfigureProject", commandRoot);
            Measure(
                metrics,
                "1. stress 15k characters command 생성 비용",
                "MainWindow stress command total",
                () =>
                {
                    RunDispatcherTask(() => scope.InvokePrivateTask("CreateStressDocumentAsync"));
                    return commandRoot;
                },
                "button command path: create + save + binder refresh + load segment");
        }

        using (var scope = new MainWindowProbeScope())
        {
            var editor = scope.Find<TextBox>("EditorBox");
            Measure(
                metrics,
                "3. EditorBox에 텍스트 주입하는 비용",
                "MainWindow EditorBox segment set",
                () =>
                {
                    editor.Text = editorView.Text;
                    return editor.Text.Length;
                },
                $"{editorView.Text.Length:N0} chars visible segment");
        }

        var fullBaselineEditor = CreateLargeTextBox();
        Measure(
            metrics,
            "3. EditorBox에 텍스트 주입하는 비용",
            "Standalone TextBox full set baseline",
            () =>
            {
                fullBaselineEditor.Text = fullText;
                return fullBaselineEditor.Text.Length;
            },
            $"{fullText.Length:N0} chars full text baseline");

        using (var scope = new MainWindowProbeScope())
        {
            var editor = scope.Find<TextBox>("EditorBox");
            editor.Text = editorView.Text;
            Measure(
                metrics,
                "4. WPF 실제 렌더/layout 비용",
                "MainWindow segment layout/render",
                () =>
                {
                    ForceLayoutAndRender(scope.Window);
                    return editor.Text.Length;
                },
                "main workbench visual tree, segment text");
        }

        var fullRenderWindow = CreateProbeWindow(fullText);
        try
        {
            Measure(
                metrics,
                "4. WPF 실제 렌더/layout 비용",
                "Standalone full TextBox layout/render",
                () =>
                {
                    ForceLayoutAndRender(fullRenderWindow);
                    return fullText.Length;
                },
                "forced render pass for full 15k plain text");
        }
        finally
        {
            fullRenderWindow.Close();
        }

        Measure(
            metrics,
            "6. preview/render 전환 비용",
            "PreviewTextService.CreatePreview",
            () => PreviewTextService.CreatePreview(fullText),
            "snapshot text generation only");

        using (var scope = new MainWindowProbeScope())
        {
            var editor = scope.Find<TextBox>("EditorBox");
            editor.Text = fullText;
            Measure(
                metrics,
                "6. preview/render 전환 비용",
                "MainWindow preview switch/render",
                () =>
                {
                    scope.InvokePrivate("ShowPreviewSurface");
                    ForceLayoutAndRender(scope.Window);
                    return scope.Find<TextBlock>("PreviewText").Text.Length;
                },
                "full text source, bounded preview surface");
        }

        Measure(
            metrics,
            "7. 분리창 동기화 비용",
            "DetachedDocumentWindow construct",
            () =>
            {
                var window = new DetachedDocumentWindow(store, loaded);
                try
                {
                    ForceLayoutAndRender(window);
                    return window.BodyText.Length;
                }
                finally
                {
                    window.Close();
                }
            },
            "full detached text load + first layout/render");

        var detachedAppendWindow = new DetachedDocumentWindow(store, loaded);
        try
        {
            var editor = (TextBox)detachedAppendWindow.FindName("DetachedEditorBox");
            editor.CaretIndex = editor.Text.Length;
            Measure(
                metrics,
                "7. 분리창 동기화 비용",
                "Detached append sync",
                () =>
                {
                    editor.SelectedText = " appended";
                    return editor.Text.Length;
                },
                "TextChanged sync path after metrics debounce fix");
        }
        finally
        {
            detachedAppendWindow.Close();
        }

        var detachedSaveWindow = new DetachedDocumentWindow(store, loaded)
        {
            BodyText = fullText + " appended"
        };
        try
        {
            Measure(
                metrics,
                "7. 분리창 동기화 비용",
                "Detached SaveAsync after edit",
                () =>
                {
                    RunDispatcherTask(() => detachedSaveWindow.SaveAsync(CancellationToken.None));
                    return detachedSaveWindow.StatusDisplay;
                },
                "snapshot merge + ProjectStore save");
        }
        finally
        {
            detachedSaveWindow.Close();
        }

        PrintReport(metrics, root);
        return 0;
    }

    private static T Measure<T>(
        List<ProbeMetric> metrics,
        string area,
        string name,
        Func<T> action,
        string notes)
    {
        Stabilize();
        var stopwatch = Stopwatch.StartNew();
        var result = action();
        stopwatch.Stop();
        metrics.Add(new ProbeMetric(area, name, stopwatch.Elapsed.TotalMilliseconds, notes));
        return result;
    }

    private static void MeasureTask(
        List<ProbeMetric> metrics,
        string area,
        string name,
        Func<Task> action,
        string notes)
    {
        MeasureTask(
            metrics,
            area,
            name,
            () =>
            {
                action().GetAwaiter().GetResult();
                return Task.FromResult(true);
            },
            notes);
    }

    private static T MeasureTask<T>(
        List<ProbeMetric> metrics,
        string area,
        string name,
        Func<Task<T>> action,
        string notes)
    {
        Stabilize();
        var stopwatch = Stopwatch.StartNew();
        var result = action().GetAwaiter().GetResult();
        stopwatch.Stop();
        metrics.Add(new ProbeMetric(area, name, stopwatch.Elapsed.TotalMilliseconds, notes));
        return result;
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        var tempPath = targetPath + ".tmp";
        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 64 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            await stream.WriteAsync(bytes, CancellationToken.None);
            await stream.FlushAsync(CancellationToken.None);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }

    private static TextBox CreateLargeTextBox()
    {
        return new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            UndoLimit = 0,
            TextWrapping = TextWrapping.NoWrap,
            SpellCheck = { IsEnabled = false },
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontSize = 18,
            Padding = new Thickness(28)
        };
    }

    private static Window CreateProbeWindow(string text)
    {
        var editor = CreateLargeTextBox();
        editor.Text = text;
        return new Window
        {
            Width = ProbeWindowSize.Width,
            Height = ProbeWindowSize.Height,
            Content = editor
        };
    }

    private static void ForceLayoutAndRender(Window window)
    {
        window.Width = ProbeWindowSize.Width;
        window.Height = ProbeWindowSize.Height;
        window.Measure(ProbeWindowSize);
        window.Arrange(new Rect(ProbeWindowSize));
        window.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            (int)ProbeWindowSize.Width,
            (int)ProbeWindowSize.Height,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(window);
    }

    private static void PrintReport(IEnumerable<ProbeMetric> metrics, string root)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("# Writer Workbench large pipeline perf probe");
        Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Temp project: {root}");
        Console.WriteLine();
        Console.WriteLine("| Area | Metric | ms | Notes |");
        Console.WriteLine("|---|---:|---:|---|");
        foreach (var metric in metrics)
        {
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "| {0} | {1} | {2:N3} | {3} |",
                metric.Area,
                metric.Name,
                metric.Milliseconds,
                metric.Notes));
        }
    }

    private static void Stabilize()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void RunDispatcherTask(Func<Task> action)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var frame = new DispatcherFrame();
        Exception? exception = null;
        var previousContext = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        dispatcher.BeginInvoke(async () =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                frame.Continue = false;
            }
        });

        try
        {
            Dispatcher.PushFrame(frame);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        if (exception is not null)
        {
            throw exception;
        }
    }

    private sealed class MainWindowProbeScope : IDisposable
    {
        public MainWindowProbeScope()
        {
            Window = new MainWindow
            {
                Width = ProbeWindowSize.Width,
                Height = ProbeWindowSize.Height
            };
        }

        public MainWindow Window { get; }

        public T Find<T>(string name)
            where T : class
        {
            return (T)Window.FindName(name);
        }

        public object? InvokePrivate(string methodName, params object?[] args)
        {
            var method = typeof(MainWindow).GetMethod(
                methodName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method is null)
            {
                throw new MissingMethodException(nameof(MainWindow), methodName);
            }

            return method.Invoke(Window, args.Length == 0 ? null : args);
        }

        public Task InvokePrivateTask(string methodName, params object?[] args)
        {
            var result = InvokePrivate(methodName, args);
            return result as Task ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            Window.Close();
        }
    }

    private sealed record ProbeMetric(string Area, string Name, double Milliseconds, string Notes);
}
