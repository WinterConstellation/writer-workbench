using System.Windows.Controls;

namespace WriterWorkbench.Tests;

public sealed class SceneRenameDialogTests
{
    [Fact]
    public void SceneRenameDialogShowsInitialTitleAndReturnsTrimmedTitle()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dialog = new SceneRenameDialog("scene-0007", "  Old title  ");
                var titleBox = (TextBox)dialog.FindName("SceneTitleBox");

                Assert.Equal("  Old title  ", titleBox.Text);
                titleBox.Text = "  New title  ";

                Assert.Equal("New title", dialog.SceneTitle);
                dialog.Close();
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
