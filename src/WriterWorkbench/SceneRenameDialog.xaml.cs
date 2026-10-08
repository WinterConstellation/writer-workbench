using System.Windows;

namespace WriterWorkbench;

public partial class SceneRenameDialog : Window
{
    public SceneRenameDialog(string sceneId, string title)
    {
        InitializeComponent();
        SceneIdText.Text = sceneId;
        SceneTitleBox.Text = title;
        SceneTitleBox.Focus();
        SceneTitleBox.SelectAll();
    }

    public string SceneTitle => SceneTitleBox.Text.Trim();

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SceneTitle))
        {
            System.Windows.MessageBox.Show(
                this,
                "장면 이름을 입력하세요.",
                "장면 이름 변경",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
