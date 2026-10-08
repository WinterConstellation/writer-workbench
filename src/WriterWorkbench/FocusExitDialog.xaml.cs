using System.Windows;

namespace WriterWorkbench;

public partial class FocusExitDialog : Window
{
    public FocusExitDialog()
    {
        InitializeComponent();
    }

    public string ConfirmationText => ConfirmationBox.Text;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
