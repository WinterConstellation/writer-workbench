using System.Windows.Controls;
using WriterWorkbench.Core.Help;

namespace WriterWorkbench.Tests;

public sealed class HelpWindowTests
{
    [Fact]
    public void HelpWindowListsHelpCatalogTopics()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new HelpWindow();
                var grid = Assert.IsType<DataGrid>(window.FindName("HelpTopicGrid"));

                Assert.Equal(HelpCatalog.All.Count, grid.Items.Count);
                Assert.Contains(HelpCatalog.All, topic => topic.Item == "편집기");
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
}
