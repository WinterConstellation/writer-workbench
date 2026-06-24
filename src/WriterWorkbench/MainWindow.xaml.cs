using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Appearance;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Export;
using WriterWorkbench.Core.Focus;
using WriterWorkbench.Core.Progress;
using WriterWorkbench.Core.Snapshots;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;
using WriterWorkbench.Core.Workspace;
using Forms = System.Windows.Forms;

namespace WriterWorkbench;

public partial class MainWindow : Window
{
    private string _projectRoot = Path.Combine(@"C:\WriterWorkbench\Projects", "Sample.writerproj");
    private static readonly TimeSpan AutosaveIdleDelay = TimeSpan.FromSeconds(3);
    private readonly DispatcherTimer _autosaveTimer;
    private readonly DispatcherTimer _focusTimer;
    private readonly List<double> _remainingSecondsSamples = [];
    private readonly FocusSessionService _focusSession = new();
    private readonly CommandRegistry _commandRegistry = AppCommandCatalog.CreateDefaultRegistry();
    private readonly Dictionary<string, Func<Task>> _commandHandlers = [];
    private readonly AppSessionStateService _sessionStateService = new(AppSessionStateService.DefaultPath);
    private ShortcutManager _shortcutManager = ShortcutProfileService.CreateDefaultManager();
    private WorkspacePresetService _workspacePresets;
    private ShortcutProfileService _shortcuts;
    private AppSessionState _sessionState = AppSessionState.Empty;
    private GraphicPreset _graphicPreset = GraphicPresetCatalog.GetOrDefault(null);
    private ProjectStore _store;
    private SceneMetadataStore _metadataStore;
    private ManuscriptExportService _exportService;
    private SceneSnapshotService _snapshotService;
    private StoryStructureStore _storyStructureStore;
    private string _activeDocumentId = "scene-0001";
    private WriterDocument? _activeDocument;
    private SceneMetadata? _activeSceneMetadata;
    private bool _dirty;
    private bool _saveInProgress;
    private bool _focusMode;
    private bool _loadingDocument;
    private bool _autosaveEnabled = true;
    private bool _longOperationInProgress;
    private bool _previewMode;
    private bool _suppressGraphicPresetChange;
    private bool _startupStateLoaded;
    private int? _lastAppliedPresetSlot;
    private DateTimeOffset _lastEditAt = DateTimeOffset.MinValue;
    private DocumentEditorTextView _editorTextView = DocumentEditorTextView.Empty;
    private DateTimeOffset _focusEndsAt;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private ResizeMode _previousResizeMode;

    public MainWindow()
    {
        InitializeComponent();

        var projectPaths = ProjectPaths.ForRoot(_projectRoot);
        _store = new ProjectStore(projectPaths);
        _metadataStore = new SceneMetadataStore(projectPaths);
        _exportService = new ManuscriptExportService(projectPaths, _store, _metadataStore);
        _snapshotService = new SceneSnapshotService(projectPaths, _store);
        _storyStructureStore = new StoryStructureStore(projectPaths);
        _workspacePresets = new WorkspacePresetService(projectPaths.WorkspacePresetsPath);
        _shortcuts = new ShortcutProfileService(projectPaths.ShortcutsPath);
        ProjectPathText.Text = _projectRoot;
        StatusText.Text = "프로젝트 불러오는 중...";
        GraphicPresetBox.ItemsSource = GraphicPresetCatalog.All;
        InspectorStatusBox.ItemsSource = SceneStatusOption.All;
        InspectorStatusBox.SelectedValue = SceneStatus.Draft;
        StoryNodeKindBox.ItemsSource = StoryNodeKindOption.All;
        StoryNodeKindBox.Text = "PlotPoint";
        StoryRelationshipKindBox.ItemsSource = RelationshipKindOption.All;
        StoryRelationshipKindBox.Text = "related";
        RegisterCommandHandlers();

        Loaded += async (_, _) => await InitializeProjectAsync();
        Closing += async (_, _) => await PersistSessionStateAsync();

        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _autosaveTimer.Tick += async (_, _) =>
        {
            if (AutosavePolicy.ShouldAutosave(
                    _autosaveEnabled,
                    _dirty,
                    _loadingDocument,
                    _saveInProgress,
                    _lastEditAt,
                    DateTimeOffset.Now,
                    AutosaveIdleDelay))
            {
                await SaveDocumentAsync("자동저장");
            }
        };
        _autosaveTimer.Start();

        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _focusTimer.Tick += (_, _) => UpdateFocusCountdown();
    }

    private async Task InitializeProjectAsync()
    {
        try
        {
            await LoadStartupStateAsync();
            ApplyGraphicPreset(GraphicPresetCatalog.GetOrDefault(_sessionState.GraphicPresetId));
            var title = Path.GetFileNameWithoutExtension(_projectRoot);
            var manifest = await _store.CreateProjectAsync(title, CancellationToken.None);
            await _workspacePresets.LoadAsync(CancellationToken.None);
            _shortcutManager = await _shortcuts.LoadOrCreateDefaultAsync(CancellationToken.None);
            var startupPreset = _workspacePresets.GetStartupPreset();
            var lastPreset = _sessionState.PresetSlot is int slot
                ? _workspacePresets.Get(slot)
                : null;
            var presetToApply = startupPreset ?? lastPreset;
            if (presetToApply is not null)
            {
                ApplyPreset(presetToApply);
                _lastAppliedPresetSlot = presetToApply.Slot;
            }

            UpdateStartupPresetButton();
            await RefreshBinderAsync(manifest);
            await RefreshStoryStructureAsync();

            var startupDocument = ResolveStartupDocument(manifest);
            if (startupDocument is not null)
            {
                await LoadDocumentAsync(startupDocument.Id, _sessionState.Surface);
            }
            else
            {
                UpdateMainSurface(manifest, []);
            }

            StatusText.Text = presetToApply is null
                ? $"프로젝트 준비됨 - 문서 {manifest.Documents.Count:N0}개"
                : $"프로젝트 준비됨 - 문서 {manifest.Documents.Count:N0}개, 프리셋 {presetToApply.Slot} 적용";
            await PersistSessionStateAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"프로젝트 불러오기 실패: {ex.Message}";
        }
    }

    private async Task LoadStartupStateAsync()
    {
        if (_startupStateLoaded)
        {
            return;
        }

        _sessionState = await _sessionStateService.LoadAsync(CancellationToken.None);
        _startupStateLoaded = true;
        if (!string.IsNullOrWhiteSpace(_sessionState.ProjectRoot) &&
            Directory.Exists(_sessionState.ProjectRoot))
        {
            ConfigureProject(_sessionState.ProjectRoot);
        }
    }

    private ProjectDocumentInfo? ResolveStartupDocument(ProjectManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(_sessionState.DocumentId))
        {
            var lastDocument = manifest.Documents.FirstOrDefault(document =>
                string.Equals(document.Id, _sessionState.DocumentId, StringComparison.OrdinalIgnoreCase));
            if (lastDocument is not null)
            {
                return lastDocument;
            }
        }

        return manifest.Documents.FirstOrDefault();
    }

    private async Task RefreshBinderAsync(ProjectManifest? manifest = null)
    {
        manifest ??= await _store.LoadManifestAsync(CancellationToken.None);
        var items = await CreateDocumentListItemsAsync(manifest.Documents);
        BinderList.ItemsSource = items;
        UpdateMainSurface(manifest, items);
    }

    private async Task<IReadOnlyList<DocumentListItem>> CreateDocumentListItemsAsync(IEnumerable<ProjectDocumentInfo> documents)
    {
        var items = new List<DocumentListItem>();
        foreach (var document in documents)
        {
            var metadata = await _metadataStore.LoadExistingOrDefaultAsync(document.Id, CancellationToken.None);
            items.Add(DocumentListItem.From(document, metadata));
        }

        return items;
    }

    private void UpdateMainSurface(ProjectManifest manifest, IReadOnlyList<DocumentListItem> items)
    {
        MainProjectTitleText.Text = manifest.Title;
        MainProjectPathText.Text = _projectRoot;
        MainRecentList.ItemsSource = items
            .OrderByDescending(document => document.UpdatedAt)
            .ThenBy(document => document.Id, StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();
    }

    private void SelectBinderItem(string documentId)
    {
        foreach (var item in BinderList.Items.OfType<DocumentListItem>())
        {
            if (item.Id == documentId)
            {
                BinderList.SelectedItem = item;
                return;
            }
        }
    }

    private async void BinderList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (BinderList.SelectedItem is not DocumentListItem item || item.Id == _activeDocumentId)
        {
            return;
        }

        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        await LoadDocumentAsync(item.Id);
    }

    private async void SearchResultsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is not SearchResultListItem item)
        {
            return;
        }

        await SelectDocumentAsync(item.DocumentId);
    }

    private async void MainRecentList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MainRecentList.SelectedItem is not DocumentListItem item)
        {
            return;
        }

        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        await SelectDocumentAsync(item.Id);
    }

    private async Task SelectDocumentAsync(string documentId)
    {
        foreach (var item in BinderList.Items.OfType<DocumentListItem>())
        {
            if (item.Id == documentId)
            {
                if (BinderList.SelectedItem is DocumentListItem selected && selected.Id == documentId)
                {
                    await LoadDocumentAsync(item.Id);
                    return;
                }

                BinderList.SelectedItem = item;
                return;
            }
        }
    }

    private async Task LoadDocumentAsync(string documentId, string? startupSurface = null)
    {
        try
        {
            _loadingDocument = true;
            var document = await Task.Run(() => _store.LoadDocumentAsync(documentId, CancellationToken.None));
            LongOperationProgressTracker? tracker = null;
            if (!_longOperationInProgress && document.Paragraphs.Count >= 1_000)
            {
                tracker = new LongOperationProgressTracker($"불러오기 {document.Id}", 3);
                BeginLongOperation($"불러오기 {document.Id}");
                ReportLongOperation(tracker.Report(1, "문서 읽는 중"));
            }

            _activeDocument = document;
            _activeDocumentId = document.Id;
            TitleBox.Text = document.Title;
            if (tracker is not null)
            {
                ReportLongOperation(tracker.Report(2, "편집 구간 준비 중"));
            }

            var editorView = DocumentEditorTextService.CreateView(document);
            _editorTextView = editorView;
            EditorBox.IsReadOnly = false;
            EditorBox.Text = editorView.Text;
            PreviewText.Text = "";
            UpdateMetrics(document);
            await LoadSceneMetadataAsync(document.Id);
            await RefreshSnapshotsAsync(document.Id);
            ShowRequestedSurface(startupSurface);
            _dirty = false;
            StatusText.Text = editorView.IsSegmentMode
                ? $"불러옴 {document.Id} - 대용량 편집 구간 {editorView.VisibleParagraphCount:N0}/{document.Paragraphs.Count:N0}문단"
                : $"불러옴 {document.Id}";
            if (tracker is not null)
            {
                ReportLongOperation(tracker.Report(3, "준비됨"));
                CompleteLongOperation(StatusText.Text);
            }
            await PersistSessionStateAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"불러오기 실패: {ex.Message}";
        }
        finally
        {
            _loadingDocument = false;
        }
    }

    private void ShowRequestedSurface(string? surface)
    {
        switch (surface)
        {
            case AppSessionState.PreviewSurface:
                ShowPreviewSurface();
                break;
            case AppSessionState.MainSurface:
                ShowMainSurface();
                break;
            default:
                ShowEditorSurface();
                break;
        }
    }

    private async void CommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string commandId)
        {
            return;
        }

        await ExecuteCommandAsync(commandId);
    }

    private async Task ExecuteCommandAsync(string commandId)
    {
        try
        {
            _ = _commandRegistry.Get(commandId);
        }
        catch (KeyNotFoundException)
        {
            StatusText.Text = $"알 수 없는 명령: {commandId}";
            return;
        }

        if (!_commandHandlers.TryGetValue(commandId, out var handler))
        {
            StatusText.Text = $"실행기가 없는 명령: {commandId}";
            return;
        }

        await handler();
    }

    private void RegisterCommandHandlers()
    {
        _commandHandlers[AppCommandIds.ProjectNew] = CreateNewProjectAsync;
        _commandHandlers[AppCommandIds.ProjectOpen] = OpenProjectAsync;
        _commandHandlers[AppCommandIds.ExportCurrentScene] = ExportCurrentSceneAsync;
        _commandHandlers[AppCommandIds.ExportFullManuscript] = ExportFullManuscriptAsync;
        _commandHandlers[AppCommandIds.SnapshotCreateCurrent] = CreateCurrentSnapshotAsync;
        _commandHandlers[AppCommandIds.SnapshotRestoreSelected] = RestoreSelectedSnapshotAsync;
        _commandHandlers[AppCommandIds.SnapshotDeleteSelected] = DeleteSelectedSnapshotAsync;
        _commandHandlers[AppCommandIds.StoryAddNode] = AddStoryNodeAsync;
        _commandHandlers[AppCommandIds.StoryAddRelationship] = AddStoryRelationshipAsync;
        _commandHandlers[AppCommandIds.DocumentCreateScene] = CreateNewSceneAsync;
        _commandHandlers[AppCommandIds.DocumentCreateStressLarge] = CreateStressDocumentAsync;
        _commandHandlers[AppCommandIds.DocumentDetachCurrent] = DetachCurrentDocumentAsync;
        _commandHandlers[AppCommandIds.DocumentRenameScene] = RenameSelectedSceneAsync;
        _commandHandlers[AppCommandIds.DocumentDuplicateScene] = DuplicateSelectedSceneAsync;
        _commandHandlers[AppCommandIds.DocumentDeleteScene] = DeleteSelectedSceneAsync;
        _commandHandlers[AppCommandIds.DocumentMoveSceneUp] = () => MoveSelectedSceneAsync(-1);
        _commandHandlers[AppCommandIds.DocumentMoveSceneDown] = () => MoveSelectedSceneAsync(1);
        _commandHandlers[AppCommandIds.ProjectSave] = () => SaveDocumentAsync("저장됨");
        _commandHandlers[AppCommandIds.WritingFocusToggle] = () =>
        {
            ToggleFocus();
            return Task.CompletedTask;
        };
        _commandHandlers[AppCommandIds.WorkspacePresetOne] = () => ApplyOrSavePresetAsync(1);
        _commandHandlers[AppCommandIds.WorkspacePresetTwo] = () => ApplyOrSavePresetAsync(2);
        _commandHandlers[AppCommandIds.WorkspacePresetThree] = () => ApplyOrSavePresetAsync(3);
        _commandHandlers[AppCommandIds.WorkspaceStartupPresetCycle] = CycleStartupPresetAsync;
        _commandHandlers[AppCommandIds.ShortcutsOpenSettings] = OpenShortcutSettingsAsync;
        _commandHandlers[AppCommandIds.ViewMainOpen] = OpenMainSurfaceAsync;
        _commandHandlers[AppCommandIds.ViewPreviewToggle] = TogglePreviewAsync;
        _commandHandlers[AppCommandIds.SearchRun] = RunSearchAsync;
        _commandHandlers[AppCommandIds.HelpOpen] = OpenHelpWindowAsync;
        _commandHandlers[AppCommandIds.AutosaveToggle] = () =>
        {
            ToggleAutosave();
            return Task.CompletedTask;
        };
    }

    private async Task CreateNewProjectAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        var root = Path.Combine(
            @"C:\WriterWorkbench\Projects",
            $"Project-{DateTime.Now:yyyyMMdd-HHmmss}.writerproj");
        ConfigureProject(root);
        await InitializeProjectAsync();
    }

    private async Task OpenProjectAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = ".writerproj 프로젝트 폴더 선택",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(@"C:\WriterWorkbench\Projects")
                ? @"C:\WriterWorkbench\Projects"
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        ConfigureProject(dialog.SelectedPath);
        await InitializeProjectAsync();
    }

    private void ConfigureProject(string root)
    {
        _projectRoot = root;
        var projectPaths = ProjectPaths.ForRoot(_projectRoot);
        _store = new ProjectStore(projectPaths);
        _metadataStore = new SceneMetadataStore(projectPaths);
        _exportService = new ManuscriptExportService(projectPaths, _store, _metadataStore);
        _snapshotService = new SceneSnapshotService(projectPaths, _store);
        _storyStructureStore = new StoryStructureStore(projectPaths);
        _workspacePresets = new WorkspacePresetService(projectPaths.WorkspacePresetsPath);
        _shortcuts = new ShortcutProfileService(projectPaths.ShortcutsPath);
        _activeDocumentId = "scene-0001";
        _activeDocument = null;
        _activeSceneMetadata = null;
        _editorTextView = DocumentEditorTextView.Empty;
        _previewMode = false;
        _lastAppliedPresetSlot = null;
        _dirty = false;
        BinderList.ItemsSource = null;
        SearchResultsList.ItemsSource = null;
        SnapshotList.ItemsSource = null;
        StoryNodeList.ItemsSource = null;
        StoryRelationshipList.ItemsSource = null;
        StoryRelationshipSourceBox.ItemsSource = null;
        StoryRelationshipTargetBox.ItemsSource = null;
        TitleBox.Text = "";
        EditorBox.Text = "";
        EditorBox.IsReadOnly = false;
        PreviewText.Text = "";
        ClearStoryStructureInputs();
        ClearSceneInspector();
        ShowEditorSurface();
        ProjectPathText.Text = _projectRoot;
    }

    private async Task RefreshStoryStructureAsync()
    {
        try
        {
            var structure = await _storyStructureStore.LoadOrCreateAsync(CancellationToken.None);
            var nodeItems = structure.Nodes
                .Select(node => new StoryNodeListItem(node))
                .ToList();
            StoryNodeList.ItemsSource = nodeItems;
            StoryRelationshipSourceBox.ItemsSource = nodeItems;
            StoryRelationshipTargetBox.ItemsSource = nodeItems;
            StoryRelationshipList.ItemsSource = structure.Relationships
                .Select(relationship => new RelationshipListItem(relationship))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StoryNodeList.ItemsSource = null;
            StoryRelationshipList.ItemsSource = null;
            StatusText.Text = $"스토리 구조 로드 실패 - {ex.Message}";
        }
    }

    private async Task AddStoryNodeAsync()
    {
        var name = StoryNodeNameBox.Text.Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "구조 노드 이름을 입력하세요.";
            return;
        }

        try
        {
            var linkedScenes = string.IsNullOrWhiteSpace(_activeDocumentId)
                ? Array.Empty<string>()
                : [_activeDocumentId];
            var node = await _storyStructureStore.AddNodeAsync(
                name,
                ReadStoryNodeKind(),
                StoryNodeSummaryBox.Text,
                [],
                linkedScenes,
                CancellationToken.None);
            StoryNodeNameBox.Text = "";
            StoryNodeSummaryBox.Text = "";
            await RefreshStoryStructureAsync();
            StatusText.Text = $"구조 노드 추가됨 {node.Id} - {node.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"구조 노드 추가 실패 - {ex.Message}";
        }
    }

    private async Task AddStoryRelationshipAsync()
    {
        var sourceNodeId = ReadRelationshipEndpoint(StoryRelationshipSourceBox);
        var targetNodeId = ReadRelationshipEndpoint(StoryRelationshipTargetBox);
        if (sourceNodeId.Length == 0 || targetNodeId.Length == 0)
        {
            StatusText.Text = "관계의 시작/도착 노드를 선택하세요.";
            return;
        }

        try
        {
            var relationship = await _storyStructureStore.AddRelationshipAsync(
                sourceNodeId,
                targetNodeId,
                ReadRelationshipKind(),
                StoryRelationshipSummaryBox.Text,
                1,
                [],
                CancellationToken.None);
            StoryRelationshipSummaryBox.Text = "";
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계 추가됨 {relationship.Id} - {relationship.SourceNodeId} -> {relationship.TargetNodeId}";
        }
        catch (InvalidOperationException ex)
        {
            StatusText.Text = $"관계 추가 실패 - {ex.Message}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"관계 추가 실패 - {ex.Message}";
        }
    }

    private void StoryNodeList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (StoryNodeList.SelectedItem is not StoryNodeListItem item)
        {
            return;
        }

        StoryRelationshipSourceBox.SelectedItem ??= item;
    }

    private void ClearStoryStructureInputs()
    {
        StoryNodeNameBox.Text = "";
        StoryNodeSummaryBox.Text = "";
        StoryNodeKindBox.Text = "PlotPoint";
        StoryRelationshipKindBox.Text = "related";
        StoryRelationshipSummaryBox.Text = "";
    }

    private string ReadStoryNodeKind()
    {
        var kind = StoryNodeKindBox.Text.Trim();
        return kind.Length == 0 ? "PlotPoint" : kind;
    }

    private string ReadRelationshipKind()
    {
        var kind = StoryRelationshipKindBox.Text.Trim();
        return kind.Length == 0 ? "related" : kind;
    }

    private static string ReadRelationshipEndpoint(System.Windows.Controls.ComboBox comboBox)
    {
        return comboBox.SelectedItem is StoryNodeListItem item
            ? item.Id
            : comboBox.Text.Trim();
    }

    private async Task CreateNewSceneAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        var document = await _store.CreateDocumentAsync("새 장면", CancellationToken.None);
        await RefreshBinderAsync();
        await SelectDocumentAsync(document.Id);
        TitleBox.Focus();
        TitleBox.SelectAll();
        StatusText.Text = $"생성됨 {document.Id} - {document.Title}";
    }

    private async Task ExportCurrentSceneAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("내보내기 전 저장");
        }

        try
        {
            var result = await _exportService.ExportCurrentSceneAsync(_activeDocumentId, CancellationToken.None);
            StatusText.Text = $"현재 장면 내보내기 완료 포함 {result.IncludedSceneCount:N0}개, 글자 {result.CharacterCount:N0} - {result.OutputPath}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or KeyNotFoundException)
        {
            StatusText.Text = $"현재 장면 내보내기 실패 {_activeDocumentId} - {ex.Message}";
        }
    }

    private async Task ExportFullManuscriptAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("내보내기 전 저장");
        }

        try
        {
            var result = await _exportService.ExportFullManuscriptAsync(CancellationToken.None);
            StatusText.Text = $"전체 원고 내보내기 완료 포함 {result.IncludedSceneCount:N0}개, 제외 {result.ExcludedSceneCount:N0}개, 글자 {result.CharacterCount:N0} - {result.OutputPath}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or KeyNotFoundException or InvalidOperationException)
        {
            StatusText.Text = $"전체 원고 내보내기 실패 - {ex.Message}";
        }
    }

    private async Task CreateCurrentSnapshotAsync()
    {
        if (string.IsNullOrWhiteSpace(_activeDocumentId))
        {
            StatusText.Text = "스냅샷을 만들 장면이 없습니다.";
            return;
        }

        if (_dirty)
        {
            await SaveDocumentAsync("스냅샷 전 저장", offerLargeOverwriteSnapshot: false);
            if (_dirty)
            {
                StatusText.Text = $"스냅샷 생성 중단 {_activeDocumentId} - 저장되지 않은 변경이 남아 있습니다.";
                return;
            }
        }

        try
        {
            var snapshot = await _snapshotService.CreateSnapshotAsync(_activeDocumentId, "수동", CancellationToken.None);
            await RefreshSnapshotsAsync(_activeDocumentId);
            StatusText.Text = $"스냅샷 생성됨 {snapshot.DocumentId} - {snapshot.SnapshotId}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            StatusText.Text = $"스냅샷 생성 실패 {_activeDocumentId} - {ex.Message}";
        }
    }

    private async Task RestoreSelectedSnapshotAsync()
    {
        var snapshot = GetSelectedSnapshot();
        if (snapshot is null)
        {
            StatusText.Text = "복원할 스냅샷을 선택하세요.";
            return;
        }

        if (!ConfirmRestoreSnapshot(snapshot))
        {
            StatusText.Text = $"스냅샷 복원 취소 {snapshot.DocumentId} - {snapshot.SnapshotId}";
            return;
        }

        if (_dirty)
        {
            await SaveDocumentAsync("복원 전 저장", offerLargeOverwriteSnapshot: false);
            if (_dirty)
            {
                StatusText.Text = $"스냅샷 복원 중단 {snapshot.DocumentId} - 저장되지 않은 변경이 남아 있습니다.";
                return;
            }
        }

        if (!await OfferOptionalSnapshotAsync(
                snapshot.DocumentId,
                "복원 전 자동",
                $"스냅샷 복원 전에 현재 장면을 백업할까요?\n\n{snapshot.DocumentId}"))
        {
            StatusText.Text = $"스냅샷 복원 취소 {snapshot.DocumentId} - {snapshot.SnapshotId}";
            return;
        }

        try
        {
            var restored = await _snapshotService.RestoreSnapshotAsync(snapshot.DocumentId, snapshot.SnapshotId, CancellationToken.None);
            await RefreshBinderAsync();
            await SelectDocumentAsync(restored.DocumentId);
            await RefreshSnapshotsAsync(restored.DocumentId);
            StatusText.Text = $"스냅샷 복원됨 {restored.DocumentId} - {restored.Snapshot.SnapshotId}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or DirectoryNotFoundException or FileNotFoundException or JsonException)
        {
            StatusText.Text = $"스냅샷 복원 실패 {snapshot.DocumentId} - {ex.Message}";
        }
    }

    private async Task DeleteSelectedSnapshotAsync()
    {
        var snapshot = GetSelectedSnapshot();
        if (snapshot is null)
        {
            StatusText.Text = "삭제할 스냅샷을 선택하세요.";
            return;
        }

        if (!ConfirmDeleteSnapshot(snapshot))
        {
            StatusText.Text = $"스냅샷 삭제 취소 {snapshot.DocumentId} - {snapshot.SnapshotId}";
            return;
        }

        try
        {
            await _snapshotService.DeleteSnapshotAsync(snapshot.DocumentId, snapshot.SnapshotId, CancellationToken.None);
            await RefreshSnapshotsAsync(snapshot.DocumentId);
            StatusText.Text = $"스냅샷 삭제됨 {snapshot.DocumentId} - {snapshot.SnapshotId}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            StatusText.Text = $"스냅샷 삭제 실패 {snapshot.DocumentId} - {ex.Message}";
        }
    }

    private async Task RenameSelectedSceneAsync()
    {
        var documentId = GetSelectedDocumentId();
        if (documentId is null)
        {
            StatusText.Text = "이름을 바꿀 장면이 없습니다.";
            return;
        }

        if (_dirty && string.Equals(documentId, _activeDocumentId, StringComparison.OrdinalIgnoreCase))
        {
            await SaveDocumentAsync("이름 변경 전 저장");
        }

        try
        {
            var manifest = await _store.LoadManifestAsync(CancellationToken.None);
            var scene = manifest.Documents.FirstOrDefault(document =>
                string.Equals(document.Id, documentId, StringComparison.OrdinalIgnoreCase));
            if (scene is null)
            {
                StatusText.Text = $"이름 변경 실패 {documentId} - 바인더에서 찾을 수 없습니다.";
                return;
            }

            var dialog = new SceneRenameDialog(scene.Id, scene.Title)
            {
                Owner = this
            };
            if (dialog.ShowDialog() != true)
            {
                StatusText.Text = $"이름 변경 취소 {scene.Id} - {scene.Title}";
                return;
            }

            var renamed = await _store.RenameDocumentAsync(scene.Id, dialog.SceneTitle, CancellationToken.None);
            var refreshed = await _store.LoadManifestAsync(CancellationToken.None);
            await RefreshBinderAsync(refreshed);
            SelectBinderItem(renamed.Id);
            UpdateActiveDocumentTitleIfNeeded(renamed);
            StatusText.Text = $"이름 변경됨 {renamed.Id} - {renamed.Title}";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException or KeyNotFoundException)
        {
            StatusText.Text = $"이름 변경 실패 {documentId} - {ex.Message}";
        }
    }

    private async Task DuplicateSelectedSceneAsync()
    {
        var documentId = GetSelectedDocumentId();
        if (documentId is null)
        {
            StatusText.Text = "복제할 장면이 없습니다.";
            return;
        }

        if (_dirty && string.Equals(documentId, _activeDocumentId, StringComparison.OrdinalIgnoreCase))
        {
            await SaveDocumentAsync("복제 전 저장");
        }

        var duplicate = await _store.DuplicateDocumentAsync(documentId, CancellationToken.None);
        await RefreshBinderAsync();
        await SelectDocumentAsync(duplicate.Id);
        StatusText.Text = $"복제됨 {duplicate.Id} - {duplicate.Title}";
    }

    private async Task DeleteSelectedSceneAsync()
    {
        var documentId = GetSelectedDocumentId();
        if (documentId is null)
        {
            StatusText.Text = "삭제할 장면이 없습니다.";
            return;
        }

        try
        {
            var manifestBeforeDelete = await _store.LoadManifestAsync(CancellationToken.None);
            var deleteTarget = manifestBeforeDelete.Documents
                .Select((document, index) => new { Scene = document, Index = index })
                .First(item => string.Equals(item.Scene.Id, documentId, StringComparison.OrdinalIgnoreCase));
            if (!ConfirmDeleteScene(deleteTarget.Scene))
            {
                StatusText.Text = $"삭제 취소 {deleteTarget.Scene.Id} - {deleteTarget.Scene.Title}";
                return;
            }

            if (_dirty && string.Equals(documentId, _activeDocumentId, StringComparison.OrdinalIgnoreCase))
            {
                await SaveDocumentAsync("삭제 전 저장", offerLargeOverwriteSnapshot: false);
                if (_dirty)
                {
                    StatusText.Text = $"삭제 중단 {deleteTarget.Scene.Id} - 저장되지 않은 변경이 남아 있습니다.";
                    return;
                }
            }

            if (!await OfferOptionalSnapshotAsync(
                    documentId,
                    "삭제 전 자동",
                    $"장면 삭제 전에 스냅샷을 만들까요?\n\n{deleteTarget.Scene.Id} - {deleteTarget.Scene.Title}"))
            {
                StatusText.Text = $"삭제 취소 {deleteTarget.Scene.Id} - {deleteTarget.Scene.Title}";
                return;
            }

            var manifest = await _store.DeleteDocumentAsync(documentId, CancellationToken.None);
            await RefreshBinderAsync(manifest);

            var nextDocument = manifest.Documents.Count == 0
                ? null
                : manifest.Documents[Math.Min(deleteTarget.Index, manifest.Documents.Count - 1)];
            if (nextDocument is not null)
            {
                await SelectDocumentAsync(nextDocument.Id);
            }

            StatusText.Text = $"삭제됨 {deleteTarget.Scene.Id} - {deleteTarget.Scene.Title}";
        }
        catch (InvalidOperationException ex)
        {
            StatusText.Text = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            StatusText.Text = $"삭제 실패 {documentId} - {ex.Message}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            StatusText.Text = $"삭제 실패 {documentId} - {ex.Message}";
        }
    }

    private async Task MoveSelectedSceneAsync(int offset)
    {
        var documentId = GetSelectedDocumentId();
        if (documentId is null)
        {
            StatusText.Text = "이동할 장면이 없습니다.";
            return;
        }

        var manifest = await _store.MoveDocumentAsync(documentId, offset, CancellationToken.None);
        await RefreshBinderAsync(manifest);
        SelectBinderItem(documentId);
        var moved = manifest.Documents.First(document =>
            string.Equals(document.Id, documentId, StringComparison.OrdinalIgnoreCase));
        StatusText.Text = offset < 0
            ? $"위로 이동 {moved.Id} - {moved.Title}"
            : $"아래로 이동 {moved.Id} - {moved.Title}";
    }

    private bool ConfirmDeleteScene(ProjectDocumentInfo scene)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"이 장면을 삭제할까요?\n\n{scene.Id} - {scene.Title}",
            "장면 삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private bool ConfirmRestoreSnapshot(SceneSnapshotInfo snapshot)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"선택한 스냅샷으로 현재 장면을 복원할까요?\n\n{snapshot.DocumentId} - {snapshot.Title}\n{snapshot.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
            "스냅샷 복원 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private bool ConfirmDeleteSnapshot(SceneSnapshotInfo snapshot)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"이 스냅샷을 삭제할까요?\n\n{snapshot.DocumentId} - {snapshot.SnapshotId}",
            "스냅샷 삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private async Task<bool> OfferOptionalSnapshotAsync(string documentId, string reason, string message)
    {
        if (!File.Exists(ProjectPaths.ForRoot(_projectRoot).DocumentJsonPath(documentId)))
        {
            return true;
        }

        var result = System.Windows.MessageBox.Show(
            this,
            $"{message}\n\n예: 먼저 스냅샷 생성\n아니요: 그대로 진행\n취소: 작업 중단",
            "작업 전 스냅샷",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var snapshot = await _snapshotService.CreateSnapshotAsync(documentId, reason, CancellationToken.None);
                await RefreshSnapshotsAsync(documentId);
                StatusText.Text = $"작업 전 스냅샷 생성됨 {snapshot.DocumentId} - {snapshot.SnapshotId}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
            {
                StatusText.Text = $"작업 전 스냅샷 실패 {documentId} - {ex.Message}";
                return false;
            }
        }

        return true;
    }

    private SceneSnapshotInfo? GetSelectedSnapshot()
    {
        return SnapshotList.SelectedItem is SnapshotListItem item
            ? item.Snapshot
            : null;
    }

    private async Task RefreshSnapshotsAsync(string documentId)
    {
        try
        {
            var snapshots = await _snapshotService.ListSnapshotsAsync(documentId, CancellationToken.None);
            SnapshotList.ItemsSource = snapshots
                .Select(snapshot => new SnapshotListItem(snapshot))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SnapshotList.ItemsSource = null;
            StatusText.Text = $"스냅샷 목록 불러오기 실패 {documentId} - {ex.Message}";
        }
    }

    private void UpdateActiveDocumentTitleIfNeeded(WriterDocument renamed)
    {
        if (!string.Equals(renamed.Id, _activeDocumentId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _loadingDocument = true;
        try
        {
            _activeDocument = renamed;
            TitleBox.Text = renamed.Title;
            _dirty = false;
        }
        finally
        {
            _loadingDocument = false;
        }
    }

    private string? GetSelectedDocumentId()
    {
        return BinderList.SelectedItem is DocumentListItem item
            ? item.Id
            : string.IsNullOrWhiteSpace(_activeDocumentId)
                ? null
                : _activeDocumentId;
    }

    private async Task DetachCurrentDocumentAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("창 분리 전 저장");
        }

        var document = _activeDocument ?? await _store.LoadDocumentAsync(_activeDocumentId, CancellationToken.None);
        var window = new DetachedDocumentWindow(_store, document)
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = Left + 48,
            Top = Top + 48
        };

        window.Show();
        StatusText.Text = $"창 분리됨 {document.Id}";
    }

    private async Task CreateStressDocumentAsync()
    {
        if (_dirty)
        {
            await SaveDocumentAsync("자동저장");
        }

        var id = $"stress-{DateTime.Now:HHmmss}";
        var tracker = new LongOperationProgressTracker("스트레스 15k", 15_004);
        BeginLongOperation("스트레스 15k");
        ReportLongOperation(tracker.Report(0, "준비 중"));
        var stopwatch = Stopwatch.StartNew();
        IProgress<LongOperationProgress> progress = new Progress<LongOperationProgress>(ReportLongOperation);
        var paragraphProgress = new Progress<int>(completed =>
            progress.Report(tracker.Report(completed, "문단 생성 중")));

        var document = await Task.Run(async () =>
        {
            var created = LargeDocumentFactory.Create(id, "스트레스 15k", 15_000, paragraphProgress);
            progress.Report(tracker.Report(15_001, "프로젝트 파일과 검색 색인 저장 중"));
            await _store.SaveDocumentAsync(created, CancellationToken.None);
            return created;
        });

        ReportLongOperation(tracker.Report(15_002, "바인더 새로고침 중"));
        stopwatch.Stop();
        await RefreshBinderAsync();
        ReportLongOperation(tracker.Report(15_003, "편집창 불러오는 중"));
        await SelectDocumentAsync(document.Id);
        ReportLongOperation(tracker.Report(15_004, "준비됨"));
        CompleteLongOperation($"생성됨 {document.Id} - {stopwatch.ElapsedMilliseconds} ms");
        StatusText.Text = $"생성됨 {document.Id} - {stopwatch.ElapsedMilliseconds} ms";
    }

    private async void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        await ExecuteCommandAsync(AppCommandIds.ProjectSave);
        e.Handled = true;
    }

    private async Task RunSearchAsync()
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0)
        {
            SearchResultsList.ItemsSource = null;
            StatusText.Text = "검색어가 비어 있습니다.";
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var results = await Task.Run(() => _store.SearchAsync(query, CancellationToken.None));
        stopwatch.Stop();
        SearchResultsList.ItemsSource = results
            .Select(hit => new SearchResultListItem(hit.DocumentId, hit.Title, hit.Snippet))
            .ToList();
        StatusText.Text = $"검색 결과 {results.Count:N0}개 - {stopwatch.ElapsedMilliseconds} ms";
    }

    private async Task ApplyOrSavePresetAsync(int slot)
    {
        var existing = _workspacePresets.Get(slot);
        var overwrite = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (existing is null || overwrite)
        {
            var preset = CapturePreset(slot);
            await _workspacePresets.SaveAsync(preset, CancellationToken.None);
            _lastAppliedPresetSlot = slot;
            RememberSessionState(_sessionState.Surface);
            await PersistSessionStateAsync();
            UpdateStartupPresetButton();
            StatusText.Text = $"프리셋 {slot} 저장됨";
            return;
        }

        ApplyPreset(existing);
        await PersistSessionStateAsync();
        StatusText.Text = $"프리셋 {slot} 적용됨";
    }

    private async Task CycleStartupPresetAsync()
    {
        var savedSlots = _workspacePresets.GetAll()
            .Select(preset => preset.Slot)
            .Order()
            .ToList();

        if (savedSlots.Count == 0)
        {
            StatusText.Text = "먼저 프리셋을 저장하세요.";
            return;
        }

        var currentSlot = _workspacePresets.GetStartupPreset()?.Slot;
        int? nextSlot = currentSlot is null
            ? savedSlots[0]
            : savedSlots.Where(slot => slot > currentSlot).Cast<int?>().FirstOrDefault();

        await _workspacePresets.SetStartupPresetAsync(nextSlot, CancellationToken.None);
        UpdateStartupPresetButton();
        StatusText.Text = nextSlot is null
            ? "실행 시 프리셋 적용 꺼짐"
            : $"실행 시 프리셋 {nextSlot} 적용";
    }

    private async Task OpenShortcutSettingsAsync()
    {
        var window = new ShortcutSettingsWindow(_commandRegistry, _shortcutManager)
        {
            Owner = this
        };

        if (window.ShowDialog() != true || window.UpdatedShortcutManager is null)
        {
            return;
        }

        _shortcutManager = window.UpdatedShortcutManager;
        await _shortcuts.SaveAsync(_shortcutManager, CancellationToken.None);
        StatusText.Text = "단축키 저장됨";
    }

    private Task OpenHelpWindowAsync()
    {
        var window = new HelpWindow
        {
            Owner = this
        };

        window.Show();
        StatusText.Text = "도움말 열림";
        return Task.CompletedTask;
    }

    private async void InspectorSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_activeDocumentId))
        {
            StatusText.Text = "저장할 장면 정보가 없습니다.";
            return;
        }

        if (!TryReadTargetCharacterCount(out var targetCharacterCount))
        {
            StatusText.Text = $"장면 정보 저장 실패 {_activeDocumentId} - 목표 글자 수는 숫자로 입력하세요.";
            return;
        }

        try
        {
            var metadata = new SceneMetadata(
                1,
                _activeDocumentId,
                InspectorSynopsisBox.Text.Trim(),
                InspectorStatusBox.SelectedValue is SceneStatus status ? status : SceneStatus.Draft,
                ParseInspectorTags(),
                targetCharacterCount,
                DateTimeOffset.UtcNow,
                ContentLength: _activeSceneMetadata?.ContentLength ?? 0,
                ContentLengthWithSpaces: _activeSceneMetadata?.ContentLengthWithSpaces ?? 0,
                SceneType: ReadInspectorSceneType(),
                ManualLineBreak: InspectorManualLineBreakBox.IsChecked == true,
                CreatedAt: _activeSceneMetadata?.CreatedAt ?? DateTimeOffset.UtcNow,
                Summary: InspectorSynopsisBox.Text.Trim());

            await _metadataStore.SaveAsync(metadata, CancellationToken.None);
            _activeSceneMetadata = metadata;
            PopulateSceneInspector(metadata);
            StatusText.Text = $"장면 정보 저장됨 {metadata.DocumentId} - {FormatSceneStatus(metadata.Status)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"장면 정보 저장 실패 {_activeDocumentId} - {ex.Message}";
        }
    }

    private async Task LoadSceneMetadataAsync(string documentId)
    {
        try
        {
            var metadata = await _metadataStore.LoadAsync(documentId, CancellationToken.None);
            _activeSceneMetadata = metadata;
            PopulateSceneInspector(metadata);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            ClearSceneInspector();
            StatusText.Text = $"장면 정보 불러오기 실패 {documentId} - {ex.Message}";
        }
    }

    private void PopulateSceneInspector(SceneMetadata metadata)
    {
        InspectorSynopsisBox.Text = metadata.Summary;
        InspectorStatusBox.SelectedValue = metadata.Status;
        InspectorTagsBox.Text = string.Join(", ", metadata.Tags);
        InspectorTargetCountBox.Text = metadata.TargetCharacterCount?.ToString() ?? "";
        InspectorContentLengthText.Text = metadata.ContentLength.ToString("N0");
        InspectorContentLengthWithSpacesText.Text = metadata.ContentLengthWithSpaces.ToString("N0");
        InspectorSceneTypeBox.Text = metadata.SceneType;
        InspectorManualLineBreakBox.IsChecked = metadata.ManualLineBreak;
        InspectorUpdatedAtText.Text = metadata.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void ClearSceneInspector()
    {
        InspectorSynopsisBox.Text = "";
        InspectorStatusBox.SelectedValue = SceneStatus.Draft;
        InspectorTagsBox.Text = "";
        InspectorTargetCountBox.Text = "";
        InspectorCurrentCountText.Text = "0";
        InspectorContentLengthText.Text = "0";
        InspectorContentLengthWithSpacesText.Text = "0";
        InspectorSceneTypeBox.Text = "Scene";
        InspectorManualLineBreakBox.IsChecked = false;
        InspectorUpdatedAtText.Text = "-";
    }

    private bool TryReadTargetCharacterCount(out int? targetCharacterCount)
    {
        var text = InspectorTargetCountBox.Text.Trim();
        if (text.Length == 0)
        {
            targetCharacterCount = null;
            return true;
        }

        if (int.TryParse(text, out var value) && value >= 0)
        {
            targetCharacterCount = value;
            return true;
        }

        targetCharacterCount = null;
        return false;
    }

    private IReadOnlyList<string> ParseInspectorTags()
    {
        return InspectorTagsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string ReadInspectorSceneType()
    {
        var sceneType = InspectorSceneTypeBox.Text.Trim();
        return sceneType.Length == 0 ? "Scene" : sceneType;
    }

    private static string FormatSceneStatus(SceneStatus status)
    {
        return status switch
        {
            SceneStatus.Draft => "초고",
            SceneStatus.Revising => "수정중",
            SceneStatus.Final => "완료",
            SceneStatus.Excluded => "제외",
            _ => status.ToString()
        };
    }

    private async Task OpenMainSurfaceAsync()
    {
        ShowMainSurface();
        await PersistSessionStateAsync();
    }

    private Task TogglePreviewAsync()
    {
        if (_previewMode)
        {
            ShowEditorSurface();
            return PersistSessionStateAsync();
        }

        ShowPreviewSurface();
        return PersistSessionStateAsync();
    }

    private void ToggleFocus()
    {
        if (_focusMode)
        {
            TryExitFocus();
            return;
        }

        StartFocus();
    }

    private void ToggleAutosave()
    {
        _autosaveEnabled = !_autosaveEnabled;
        AutosaveButton.Content = _autosaveEnabled ? "자동저장 켬" : "자동저장 끔";
        StatusText.Text = _autosaveEnabled ? "자동저장 켜짐" : "자동저장 꺼짐. Ctrl+S 수동 저장은 유지됩니다.";
    }

    private async void GraphicPresetBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressGraphicPresetChange || GraphicPresetBox.SelectedItem is not GraphicPreset preset)
        {
            return;
        }

        ApplyGraphicPreset(preset);
        await PersistSessionStateAsync();
        StatusText.Text = $"색상 프리셋 적용됨 - {preset.Name}";
    }

    private void TitleBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_loadingDocument)
        {
            return;
        }

        _dirty = true;
        _lastEditAt = DateTimeOffset.Now;
    }

    private void EditorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_loadingDocument)
        {
            return;
        }

        _dirty = true;
        _lastEditAt = DateTimeOffset.Now;
    }

    private async Task SaveDocumentAsync(string verb, bool offerLargeOverwriteSnapshot = true)
    {
        if (_saveInProgress)
        {
            return;
        }

        try
        {
            _saveInProgress = true;
            var document = CreateCurrentDocument();
            var stopwatch = Stopwatch.StartNew();
            LongOperationProgressTracker? tracker = null;
            if (offerLargeOverwriteSnapshot &&
                ShouldOfferLargeOverwriteSnapshot(document, verb) &&
                !await OfferOptionalSnapshotAsync(
                    document.Id,
                    "대형 저장 전 자동",
                    $"대형 장면을 저장하기 전에 현재 저장본을 스냅샷으로 남길까요?\n\n{document.Id} - {document.Title}"))
            {
                StatusText.Text = $"저장 취소 {document.Id}";
                return;
            }

            if (document.Paragraphs.Count >= 1_000)
            {
                tracker = new LongOperationProgressTracker($"저장 {document.Id}", 3);
                BeginLongOperation($"저장 {document.Id}");
                ReportLongOperation(tracker.Report(0, "저장 준비 중"));
            }

            if (tracker is not null)
            {
                ReportLongOperation(tracker.Report(1, "프로젝트 파일과 검색 색인 저장 중"));
            }

            await Task.Run(() => _store.SaveDocumentAsync(document, CancellationToken.None));
            stopwatch.Stop();
            if (tracker is not null)
            {
                ReportLongOperation(tracker.Report(2, "바인더 새로고침 중"));
            }

            await RefreshBinderAsync();
            SelectBinderItem(document.Id);
            UpdateMetrics(document);
            await LoadSceneMetadataAsync(document.Id);
            _dirty = false;
            StatusText.Text = $"{verb} {DateTime.Now:HH:mm:ss} - {document.Id} - {stopwatch.ElapsedMilliseconds} ms";
            if (tracker is not null)
            {
                ReportLongOperation(tracker.Report(3, "준비됨"));
                CompleteLongOperation(StatusText.Text);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"저장 실패: {ex.Message}";
        }
        finally
        {
            _saveInProgress = false;
        }
    }

    private bool ShouldOfferLargeOverwriteSnapshot(WriterDocument document, string verb)
    {
        if (document.Paragraphs.Count < 1_000)
        {
            return false;
        }

        if (verb.Contains("자동저장", StringComparison.OrdinalIgnoreCase) ||
            verb.Contains("스냅샷 전 저장", StringComparison.OrdinalIgnoreCase) ||
            verb.Contains("복원 전 저장", StringComparison.OrdinalIgnoreCase) ||
            verb.Contains("삭제 전 저장", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return File.Exists(ProjectPaths.ForRoot(_projectRoot).DocumentJsonPath(document.Id));
    }

    private WriterDocument CreateCurrentDocument()
    {
        var existing = _activeDocument ?? new WriterDocument(_activeDocumentId, _activeDocumentId, []);
        var document = DocumentEditorTextService.UpdateFromEditorText(
            existing,
            TitleBox.Text,
            EditorBox.Text,
            _editorTextView);

        if (_editorTextView.IsSegmentMode)
        {
            _editorTextView = _editorTextView with
            {
                VisibleParagraphCount = DocumentEditorTextService.CountEditorParagraphs(EditorBox.Text)
            };
        }

        _activeDocument = document;
        return document;
    }

    private void ShowPreviewSurface()
    {
        var sourceText = _editorTextView.IsSegmentMode && _activeDocument is not null
            ? TextExportService.ToPlainText(_activeDocument)
            : EditorBox.Text;

        PreviewText.Text = PreviewTextService.CreatePreview(sourceText);
        MainSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Visible;
        PreviewModeButton.Content = "편집";
        _previewMode = true;
        RememberSessionState(AppSessionState.PreviewSurface);
        StatusText.Text = "미리보기 렌더링됨";
    }

    private void ShowEditorSurface()
    {
        MainSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Visible;
        PreviewModeButton.Content = "미리보기";
        _previewMode = false;
        RememberSessionState(AppSessionState.EditorSurface);
        EditorBox.Focus();
    }

    private void ShowMainSurface()
    {
        MainSurface.Visibility = Visibility.Visible;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        PreviewModeButton.Content = "미리보기";
        _previewMode = false;
        RememberSessionState(AppSessionState.MainSurface);
        StatusText.Text = "메인 화면";
    }

    private async void ReturnToEditorButton_Click(object sender, RoutedEventArgs e)
    {
        ShowEditorSurface();
        await PersistSessionStateAsync();
    }

    private void UpdateMetrics(WriterDocument document)
    {
        var metrics = DocumentMetricsService.Measure(document);
        MetricsText.Text =
            $"{document.Id} | 문단 {metrics.ParagraphCount:N0} | 글자 {metrics.CharacterCount:N0} | UTF-8 {metrics.PlainTextUtf8Bytes / 1024.0:N1} KB";
        InspectorCurrentCountText.Text = metrics.CharacterCount.ToString("N0");
    }

    private void BeginLongOperation(string operationName)
    {
        _longOperationInProgress = true;
        _remainingSecondsSamples.Clear();
        OperationProgressPanel.Visibility = Visibility.Visible;
        OperationProgressBar.Value = 0;
        OperationProgressText.Text = operationName;
        OperationEtaText.Text = "남은 시간 계산 중...";
        OperationRemainingGraph.Points = [];
    }

    private void ReportLongOperation(LongOperationProgress progress)
    {
        OperationProgressPanel.Visibility = Visibility.Visible;
        OperationProgressBar.Value = progress.PercentComplete;
        OperationProgressText.Text =
            $"{progress.OperationName} - {progress.Stage} ({progress.PercentComplete}%)";
        OperationEtaText.Text = progress.EstimatedRemaining is null
            ? "남은 시간 계산 중..."
            : $"남음 {FormatDuration(progress.EstimatedRemaining.Value)}";

        if (progress.EstimatedRemaining is not null)
        {
            _remainingSecondsSamples.Add(Math.Max(0, progress.EstimatedRemaining.Value.TotalSeconds));
            if (_remainingSecondsSamples.Count > 40)
            {
                _remainingSecondsSamples.RemoveAt(0);
            }

            UpdateRemainingGraph();
        }
    }

    private void CompleteLongOperation(string message)
    {
        _longOperationInProgress = false;
        OperationProgressPanel.Visibility = Visibility.Visible;
        OperationProgressBar.Value = 100;
        OperationProgressText.Text = message;
        OperationEtaText.Text = "남음 00:00";
        _remainingSecondsSamples.Add(0);
        UpdateRemainingGraph();
    }

    private void ApplyGraphicPreset(GraphicPreset preset)
    {
        _graphicPreset = preset;
        _suppressGraphicPresetChange = true;
        GraphicPresetBox.SelectedItem = preset;
        _suppressGraphicPresetChange = false;

        var windowBackground = CreateBrush(preset.WindowBackground);
        var chromeBackground = CreateBrush(preset.ChromeBackground);
        var panelBackground = CreateBrush(preset.PanelBackground);
        var editorBackground = CreateBrush(preset.EditorBackground);
        var text = CreateBrush(preset.Text);
        var mutedText = CreateBrush(preset.MutedText);
        var border = CreateBrush(preset.Border);
        var buttonBackground = CreateBrush(preset.ButtonBackground);
        var buttonText = CreateBrush(preset.ButtonText);
        var accent = CreateBrush(preset.Accent);

        Background = windowBackground;
        ApplyGraphicPresetToChildren(this, text, border, buttonBackground, buttonText, editorBackground, panelBackground);

        StatusText.Background = accent;
        StatusText.Foreground = CreateBrush("#FFFFFF");
        MetricsText.Background = panelBackground;
        MetricsText.Foreground = mutedText;
        ProjectPathText.Foreground = mutedText;
        OperationProgressPanel.Background = panelBackground;
        OperationProgressPanel.BorderBrush = border;
        OperationProgressText.Foreground = text;
        OperationEtaText.Foreground = text;
        OperationGraphCanvas.Background = editorBackground;
        OperationRemainingGraph.Stroke = accent;

        TitleBox.Background = editorBackground;
        TitleBox.Foreground = text;
        TitleBox.BorderBrush = border;
        EditorBox.Background = editorBackground;
        EditorBox.Foreground = text;
        EditorBox.CaretBrush = text;
        PreviewText.Foreground = text;
        MainSurface.Background = windowBackground;
        MainProjectPathText.Foreground = mutedText;
    }

    private static void ApplyGraphicPresetToChildren(
        DependencyObject root,
        System.Windows.Media.Brush text,
        System.Windows.Media.Brush border,
        System.Windows.Media.Brush buttonBackground,
        System.Windows.Media.Brush buttonText,
        System.Windows.Media.Brush editorBackground,
        System.Windows.Media.Brush panelBackground)
    {
        switch (root)
        {
            case System.Windows.Controls.Button button:
                button.Background = buttonBackground;
                button.Foreground = buttonText;
                button.BorderBrush = border;
                break;
            case System.Windows.Controls.ComboBox comboBox:
                comboBox.Background = buttonBackground;
                comboBox.Foreground = buttonText;
                comboBox.BorderBrush = border;
                break;
            case System.Windows.Controls.TextBox textBox:
                textBox.Background = editorBackground;
                textBox.Foreground = text;
                textBox.BorderBrush = border;
                textBox.CaretBrush = text;
                break;
            case System.Windows.Controls.ListBox listBox:
                listBox.Background = panelBackground;
                listBox.Foreground = text;
                listBox.BorderBrush = border;
                break;
            case System.Windows.Controls.TextBlock textBlock:
                textBlock.Foreground = text;
                break;
            case System.Windows.Controls.Panel panel:
                panel.Background = panelBackground;
                break;
            case System.Windows.Controls.Border borderElement:
                borderElement.BorderBrush = border;
                borderElement.Background = panelBackground;
                break;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            ApplyGraphicPresetToChildren(child, text, border, buttonBackground, buttonText, editorBackground, panelBackground);
        }
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)!);
    }

    private void UpdateRemainingGraph()
    {
        if (_remainingSecondsSamples.Count == 0)
        {
            OperationRemainingGraph.Points = [];
            return;
        }

        var width = OperationGraphCanvas.ActualWidth > 0 ? OperationGraphCanvas.ActualWidth : 190;
        var height = OperationGraphCanvas.ActualHeight > 0 ? OperationGraphCanvas.ActualHeight : 34;
        var max = Math.Max(1, _remainingSecondsSamples.Max());
        var xStep = _remainingSecondsSamples.Count == 1 ? width : width / (_remainingSecondsSamples.Count - 1);
        var points = new PointCollection();

        for (var index = 0; index < _remainingSecondsSamples.Count; index++)
        {
            var x = index * xStep;
            var y = height - ((_remainingSecondsSamples[index] / max) * height);
            points.Add(new System.Windows.Point(x, y));
        }

        OperationRemainingGraph.Points = points;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private void StartFocus()
    {
        ShowEditorSurface();
        _focusMode = true;
        var state = _focusSession.Start(new FocusSessionOptions(TimeSpan.FromMinutes(40), 20, true));
        _focusEndsAt = state.EndsAt;

        _previousWindowStyle = WindowStyle;
        _previousWindowState = WindowState;
        _previousResizeMode = ResizeMode;

        BinderColumn.Width = new GridLength(0);
        PreviewColumn.Width = new GridLength(0);
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
        Topmost = true;
        _focusTimer.Start();
        UpdateFocusCountdown();
        EditorBox.Focus();
    }

    private void TryExitFocus()
    {
        if (DateTimeOffset.Now >= _focusEndsAt)
        {
            ExitFocus();
            return;
        }

        var dialog = new FocusExitDialog { Owner = this };
        var accepted = dialog.ShowDialog() == true && _focusSession.CanExitEarly(dialog.ConfirmationText);
        if (accepted)
        {
            ExitFocus();
            return;
        }

        StatusText.Text = "집중모드 해제에는 확인 문구 20자 이상이 필요합니다.";
    }

    private void ExitFocus()
    {
        _focusMode = false;
        _focusTimer.Stop();
        BinderColumn.Width = new GridLength(280);
        PreviewColumn.Width = new GridLength(360);
        WindowStyle = _previousWindowStyle;
        WindowState = _previousWindowState;
        ResizeMode = _previousResizeMode;
        Topmost = false;
        FocusButton.Content = "집중 40:00";
        StatusText.Text = "집중 세션 종료됨";
    }

    private void UpdateFocusCountdown()
    {
        var remaining = _focusEndsAt - DateTimeOffset.Now;
        if (remaining <= TimeSpan.Zero)
        {
            FocusButton.Content = "집중 종료";
            StatusText.Text = "집중 타이머 완료. 집중 종료를 눌러 돌아가세요.";
            return;
        }

        FocusButton.Content = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        StatusText.Text = "집중모드 작동 중 - Ctrl+S 저장 가능. 조기 해제에는 20자 확인이 필요합니다.";
    }

    private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var gesture = WpfShortcutGestureFormatter.Format(e.Key, Keyboard.Modifiers);
        var scope = GetActiveCommandScope();
        var commandId = gesture is null ? null : _shortcutManager.FindCommand(gesture, scope);

        if (commandId is not null)
        {
            await ExecuteCommandAsync(commandId);
            e.Handled = true;
            return;
        }

        if (_focusMode && e.Key == Key.Escape)
        {
            TryExitFocus();
            e.Handled = true;
        }
    }

    private CommandScope GetActiveCommandScope()
    {
        if (_focusMode)
        {
            return CommandScope.FocusSession;
        }

        if (EditorBox.IsKeyboardFocusWithin || TitleBox.IsKeyboardFocusWithin)
        {
            return CommandScope.Editor;
        }

        if (BinderList.IsKeyboardFocusWithin)
        {
            return CommandScope.Binder;
        }

        if (SearchResultsList.IsKeyboardFocusWithin ||
            SearchBox.IsKeyboardFocusWithin ||
            InspectorSynopsisBox.IsKeyboardFocusWithin ||
            InspectorStatusBox.IsKeyboardFocusWithin ||
            InspectorTagsBox.IsKeyboardFocusWithin ||
            InspectorTargetCountBox.IsKeyboardFocusWithin ||
            InspectorSceneTypeBox.IsKeyboardFocusWithin ||
            InspectorManualLineBreakBox.IsKeyboardFocusWithin ||
            StoryNodeNameBox.IsKeyboardFocusWithin ||
            StoryNodeKindBox.IsKeyboardFocusWithin ||
            StoryNodeSummaryBox.IsKeyboardFocusWithin ||
            StoryNodeList.IsKeyboardFocusWithin ||
            StoryRelationshipSourceBox.IsKeyboardFocusWithin ||
            StoryRelationshipTargetBox.IsKeyboardFocusWithin ||
            StoryRelationshipKindBox.IsKeyboardFocusWithin ||
            StoryRelationshipSummaryBox.IsKeyboardFocusWithin ||
            StoryRelationshipList.IsKeyboardFocusWithin)
        {
            return CommandScope.Preview;
        }

        return CommandScope.Workbench;
    }

    private WorkspacePreset CapturePreset(int slot)
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;
        var existing = _workspacePresets.Get(slot);

        return new WorkspacePreset(
            slot,
            $"프리셋 {slot}",
            MonitorRegion.Full,
            existing?.AutoApplyOnStartup ?? false,
            new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, WindowState.ToString()));
    }

    private void ApplyPreset(WorkspacePreset preset)
    {
        if (preset.Placement is null)
        {
            return;
        }

        WindowState = WindowState.Normal;
        Left = preset.Placement.Left;
        Top = preset.Placement.Top;
        Width = Math.Max(960, preset.Placement.Width);
        Height = Math.Max(640, preset.Placement.Height);

        if (Enum.TryParse<WindowState>(preset.Placement.WindowState, out var state))
        {
            WindowState = state;
        }

        _lastAppliedPresetSlot = preset.Slot;
        RememberSessionState(_sessionState.Surface);
    }

    private void UpdateStartupPresetButton()
    {
        var startupPreset = _workspacePresets.GetStartupPreset();
        StartupPresetButton.Content = startupPreset is null ? "시작 적용 끔" : $"시작 P{startupPreset.Slot}";
    }

    private void RememberSessionState(string surface)
    {
        _sessionState = new AppSessionState(
            _projectRoot,
            string.IsNullOrWhiteSpace(_activeDocumentId) ? null : _activeDocumentId,
            surface,
            _lastAppliedPresetSlot ?? _sessionState.PresetSlot,
            _graphicPreset.Id);
    }

    private async Task PersistSessionStateAsync()
    {
        try
        {
            RememberSessionState(_sessionState.Surface);
            await _sessionStateService.SaveAsync(_sessionState, CancellationToken.None);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record DocumentListItem(
        string Id,
        string Title,
        string Status,
        string Summary,
        string Tags,
        int ContentLength,
        int ContentLengthWithSpaces,
        string SceneType,
        DateTimeOffset UpdatedAt)
    {
        public string Display =>
            $"{Id}  {Title} | {Status} | {SceneType} | {ContentLength:N0}/{ContentLengthWithSpaces:N0} | {Tags} | {Summary}";

        public static DocumentListItem From(ProjectDocumentInfo document, SceneMetadata metadata)
        {
            return new DocumentListItem(
                document.Id,
                document.Title,
                FormatSceneStatus(metadata.Status),
                metadata.Summary,
                string.Join(", ", metadata.Tags),
                metadata.ContentLength,
                metadata.ContentLengthWithSpaces,
                metadata.SceneType,
                metadata.UpdatedAt == default ? document.UpdatedAt : metadata.UpdatedAt);
        }
    }

    private sealed record SearchResultListItem(string DocumentId, string Title, string Snippet)
    {
        public string Display => $"{Title} - {Snippet}";
    }

    private sealed record SnapshotListItem(SceneSnapshotInfo Snapshot)
    {
        public string Display =>
            $"{Snapshot.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} | {Snapshot.Reason} | {Snapshot.Title}";
    }

    private sealed record StoryNodeListItem(StoryStructureNode Node)
    {
        public string Id => Node.Id;
        public string Display => $"{Node.Id} | {Node.Name} | {Node.Kind}";
    }

    private sealed record RelationshipListItem(RelationshipLink Relationship)
    {
        public string Display =>
            $"{Relationship.SourceNodeId} -> {Relationship.TargetNodeId} | {Relationship.Kind} | {Relationship.Summary}";
    }

    private sealed record StoryNodeKindOption(string Value)
    {
        public static IReadOnlyList<StoryNodeKindOption> All { get; } =
        [
            new StoryNodeKindOption("PlotPoint"),
            new StoryNodeKindOption("Arc"),
            new StoryNodeKindOption("Character"),
            new StoryNodeKindOption("Theme"),
            new StoryNodeKindOption("Place"),
            new StoryNodeKindOption("Conflict")
        ];

        public override string ToString() => Value;
    }

    private sealed record RelationshipKindOption(string Value)
    {
        public static IReadOnlyList<RelationshipKindOption> All { get; } =
        [
            new RelationshipKindOption("related"),
            new RelationshipKindOption("drives"),
            new RelationshipKindOption("blocks"),
            new RelationshipKindOption("mirrors"),
            new RelationshipKindOption("reveals"),
            new RelationshipKindOption("conflicts")
        ];

        public override string ToString() => Value;
    }

    private sealed record SceneStatusOption(SceneStatus Status, string Label)
    {
        public static IReadOnlyList<SceneStatusOption> All { get; } =
        [
            new SceneStatusOption(SceneStatus.Draft, "초고"),
            new SceneStatusOption(SceneStatus.Revising, "수정중"),
            new SceneStatusOption(SceneStatus.Final, "완료"),
            new SceneStatusOption(SceneStatus.Excluded, "제외")
        ];
    }
}
