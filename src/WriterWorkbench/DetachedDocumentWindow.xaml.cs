using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench;

public partial class DetachedDocumentWindow : Window
{
    private readonly ProjectStore _store;
    private readonly DocumentEditSession _session;
    private readonly DispatcherTimer _metricsRefreshTimer;
    private bool _loading;

    public DetachedDocumentWindow(ProjectStore store, WriterDocument document)
    {
        _store = store;
        _session = new DocumentEditSession(document);
        _metricsRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _metricsRefreshTimer.Tick += (_, _) =>
        {
            _metricsRefreshTimer.Stop();
            UpdateMetrics();
        };

        InitializeComponent();
        LoadSession();
    }

    public string DocumentTitleText
    {
        get => DetachedTitleBox.Text;
        set
        {
            DetachedTitleBox.Text = value;
            SyncSessionFromControls();
        }
    }

    public string BodyText
    {
        get => DetachedEditorBox.Text;
        set
        {
            DetachedEditorBox.Text = value;
            SyncSessionFromControls();
        }
    }

    public string MetricsDisplay => DetachedMetricsText.Text;

    public string StatusDisplay => DetachedStatusText.Text;

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        SyncSessionFromControls();
        var document = _session.CreateSnapshot();
        var stopwatch = Stopwatch.StartNew();
        await _store.SaveDocumentAsync(document, cancellationToken);
        stopwatch.Stop();

        _session.AcceptSnapshot(document);
        Title = $"분리 창 - {document.Title}";
        DetachedStatusText.Text = $"저장됨 {DateTime.Now:HH:mm:ss} - {document.Id} - {stopwatch.ElapsedMilliseconds} ms";
        _metricsRefreshTimer.Stop();
        UpdateMetrics();
    }

    private void LoadSession()
    {
        _loading = true;
        DetachedTitleBox.Text = _session.Title;
        DetachedEditorBox.Text = _session.PlainText;
        Title = $"분리 창 - {_session.Title}";
        UpdateMetrics();
        _loading = false;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await SaveAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            DetachedStatusText.Text = $"저장 실패: {ex.Message}";
        }
    }

    private void DetachedTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        SyncSessionFromControls();
        ScheduleMetricsRefresh();
        DetachedStatusText.Text = "저장되지 않은 변경";
    }

    private void SyncSessionFromControls()
    {
        _session.Title = DetachedTitleBox.Text;
        _session.PlainText = DetachedEditorBox.Text;
    }

    private void UpdateMetrics()
    {
        var metrics = _session.Measure();
        DetachedMetricsText.Text =
            $"문단 {metrics.ParagraphCount:N0} | 글자 {metrics.CharacterCount:N0} | UTF-8 {metrics.PlainTextUtf8Bytes / 1024.0:N1} KB";
    }

    private void ScheduleMetricsRefresh()
    {
        _metricsRefreshTimer.Stop();
        _metricsRefreshTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _metricsRefreshTimer.Stop();
        base.OnClosed(e);
    }
}
