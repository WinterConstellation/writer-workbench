using System.Windows;
using WriterWorkbench.Core.Application;

namespace WriterWorkbench;

public partial class App : System.Windows.Application
{
    private SingleInstanceGuard? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = SingleInstanceGuard.TryAcquire(@"Local\WriterWorkbench");
        if (_singleInstance is null)
        {
            System.Windows.MessageBox.Show(
                "원고 작업대가 이미 실행 중입니다.",
                "원고 작업대",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
