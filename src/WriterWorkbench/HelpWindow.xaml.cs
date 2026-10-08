using System.Windows;
using WriterWorkbench.Core.Help;

namespace WriterWorkbench;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        HelpTopicGrid.ItemsSource = HelpCatalog.All;
    }
}
