using System.Diagnostics;
using System.Windows.Controls;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class EditorLargeTextSmokeTests
{
    [Fact]
    public void WpfTextBoxAcceptsLargePlainTextWithinSmokeThreshold()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);
                var text = TextExportService.ToPlainText(document);
                var box = new TextBox
                {
                    AcceptsReturn = true,
                    UndoLimit = 0,
                    TextWrapping = System.Windows.TextWrapping.NoWrap
                };

                var stopwatch = Stopwatch.StartNew();
                box.Text = text;
                stopwatch.Stop();

                Assert.Contains("Large manuscript paragraph 15000", box.Text);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"TextBox load took {stopwatch.Elapsed}.");
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
    public void WpfTextBoxAppendsToLargeNoWrapTextWithinSmokeThreshold()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var document = LargeDocumentFactory.Create("scene-large", "Large", 15_000);
                var text = TextExportService.ToPlainText(document);
                var box = new TextBox
                {
                    AcceptsReturn = true,
                    UndoLimit = 0,
                    TextWrapping = System.Windows.TextWrapping.NoWrap
                };

                box.Text = text;
                box.CaretIndex = box.Text.Length;

                var stopwatch = Stopwatch.StartNew();
                box.SelectedText = " appended";
                stopwatch.Stop();

                Assert.EndsWith(" appended", box.Text, StringComparison.Ordinal);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Large TextBox append took {stopwatch.Elapsed}.");
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
}
