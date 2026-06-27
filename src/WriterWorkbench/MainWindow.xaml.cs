using System.IO;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WriterWorkbench.Core.AppSettings;
using WriterWorkbench.Core.Application;
using WriterWorkbench.Core.Appearance;
using WriterWorkbench.Core.Commands;
using WriterWorkbench.Core.Customization;
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Export;
using WriterWorkbench.Core.Focus;
using WriterWorkbench.Core.Progress;
using WriterWorkbench.Core.Snapshots;
using WriterWorkbench.Core.Storage;
using WriterWorkbench.Core.Story;
using WriterWorkbench.Core.WebWorkbench;
using WriterWorkbench.Core.Workspace;
using Forms = System.Windows.Forms;

namespace WriterWorkbench;

public partial class MainWindow : Window
{
    private string _projectRoot = Path.Combine(@"C:\WriterWorkbench\Projects", "Sample.writerproj");
    private const int DefaultFocusDurationMinutes = AppSessionState.DefaultFocusDurationMinutes;
    private const int MinFocusDurationMinutes = 1;
    private const int MaxFocusDurationMinutes = 240;
    private static readonly TimeSpan AutosaveIdleDelay = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions WebWorkbenchJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly DispatcherTimer _autosaveTimer;
    private readonly DispatcherTimer _focusTimer;
    private readonly List<double> _remainingSecondsSamples = [];
    private readonly Dictionary<string, SceneMetadata> _binderMetadataByDocumentId = new(StringComparer.OrdinalIgnoreCase);
    private readonly FocusSessionService _focusSession = new();
    private readonly CommandRegistry _commandRegistry = AppCommandCatalog.CreateDefaultRegistry();
    private readonly WorkbenchSurfaceClaimRegistry _surfaceClaims = new();
    private readonly Dictionary<string, Func<Task>> _commandHandlers = [];
    private readonly List<WorkbenchDetachedWindow> _detachedWorkbenchWindows = [];
    private readonly AppSessionStateService _sessionStateService = new(AppSessionStateService.DefaultPath);
    private ShortcutManager _shortcutManager = ShortcutProfileService.CreateDefaultManager();
    private WorkspacePresetService _workspacePresets;
    private ShortcutProfileService _shortcuts;
    private WorkbenchCustomizationProfileService _customizationProfiles;
    private ProjectSettingsStore _projectSettingsStore;
    private WidgetRegistryStore _widgetRegistryStore;
    private WorkbenchCustomizationProfile? _activeCustomizationProfile;
    private ProjectAppSettings _projectAppSettings = ProjectAppSettings.Default;
    private WorkbenchWidgetRegistry _widgetRegistry = WorkbenchWidgetRegistry.Empty;
    private AppSessionState _sessionState = AppSessionState.Empty;
    private GraphicPreset _graphicPreset = GraphicPresetCatalog.GetOrDefault(null);
    private ProjectStore _store;
    private SceneMetadataStore _metadataStore;
    private ManuscriptExportService _exportService;
    private SceneSnapshotService _snapshotService;
    private StoryStructureStore _storyStructureStore;
    private SceneEntityLinkStore _sceneEntityLinkStore;
    private ProjectManifest? _currentManifest;
    private string _activeDocumentId = "scene-0001";
    private WriterDocument? _activeDocument;
    private SceneMetadata? _activeSceneMetadata;
    private bool _dirty;
    private bool _saveInProgress;
    private bool _focusMode;
    private bool _loadingDocument;
    private bool _autosaveEnabled = true;
    private bool _longOperationInProgress;
    private string _htmlActiveView = "editor";
    private bool _suppressGraphicPresetChange;
    private bool _startupStateLoaded;
    private int? _lastAppliedPresetSlot;
    private int _focusDurationMinutes = DefaultFocusDurationMinutes;
    private DateTimeOffset _lastEditAt = DateTimeOffset.MinValue;
    private DocumentEditorTextView _editorTextView = DocumentEditorTextView.Empty;
    private DateTimeOffset _focusEndsAt;
    private WindowStyle _previousWindowStyle;
    private WindowState _previousWindowState;
    private ResizeMode _previousResizeMode;
    private RemoteControlLayerWindow? _remoteControlLayer;
    private bool _htmlWorkbenchInitialized;
    private string? _draggedRelationshipMapEntityId;
    private System.Windows.Point _relationshipMapDragOffset;

    public MainWindow()
    {
        InitializeComponent();

        var projectPaths = ProjectPaths.ForRoot(_projectRoot);
        _store = new ProjectStore(projectPaths);
        _metadataStore = new SceneMetadataStore(projectPaths);
        _exportService = new ManuscriptExportService(projectPaths, _store, _metadataStore);
        _snapshotService = new SceneSnapshotService(projectPaths, _store);
        _storyStructureStore = new StoryStructureStore(projectPaths);
        _sceneEntityLinkStore = new SceneEntityLinkStore(projectPaths);
        _workspacePresets = new WorkspacePresetService(projectPaths.WorkspacePresetsPath);
        _shortcuts = new ShortcutProfileService(projectPaths.ShortcutsPath);
        _customizationProfiles = new WorkbenchCustomizationProfileService(projectPaths.WorkbenchProfilesPath, _commandRegistry);
        _projectSettingsStore = new ProjectSettingsStore(projectPaths.AppSettingsPath);
        _widgetRegistryStore = new WidgetRegistryStore(projectPaths.WidgetRegistryPath);
        ProjectPathText.Text = _projectRoot;
        StatusText.Text = "프로젝트 불러오는 중...";
        GraphicPresetBox.ItemsSource = GraphicPresetCatalog.All;
        InspectorStatusBox.ItemsSource = SceneStatusOption.All;
        InspectorStatusBox.SelectedValue = SceneStatus.Draft;
        StoryNodeKindBox.ItemsSource = StoryEntityTypeOption.All;
        StoryNodeKindBox.Text = StoryEntityType.Character.ToString();
        RelationshipEntityTypeBox.ItemsSource = StoryEntityTypeOption.All;
        RelationshipEntityTypeBox.Text = StoryEntityType.Character.ToString();
        StoryRelationshipKindBox.ItemsSource = RelationshipKindOption.All;
        StoryRelationshipKindBox.Text = "관계";
        RegisterCommandHandlers();
        RenderRemoteControlLayer(WorkbenchCustomizationProfileFactory.CreateDefault(
            "profile-shell-default",
            "기본 리모콘",
            _commandRegistry));

        Loaded += async (_, _) => await InitializeProjectAsync();
        Closing += async (_, _) => await PersistSessionStateAsync();
        Closing += (_, _) => _remoteControlLayer?.Close();

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
            ApplyFocusDurationMinutes(_sessionState.FocusDurationMinutes);
            ApplyGraphicPreset(GraphicPresetCatalog.GetOrDefault(_sessionState.GraphicPresetId));
            var title = Path.GetFileNameWithoutExtension(_projectRoot);
            var manifest = await _store.CreateProjectAsync(title, CancellationToken.None);
            await _workspacePresets.LoadAsync(CancellationToken.None);
            _shortcutManager = await _shortcuts.LoadOrCreateDefaultAsync(CancellationToken.None);
            _activeCustomizationProfile = await _customizationProfiles.LoadOrCreateActiveProfileAsync(CancellationToken.None);
            _projectAppSettings = await _projectSettingsStore.LoadOrCreateAsync(CancellationToken.None);
            _autosaveEnabled = _projectAppSettings.AutosaveEnabled;
            AutosaveButton.Content = _autosaveEnabled ? "자동저장 켬" : "자동저장 끔";
            _sessionState = StartupSurfaceResolver.ApplyProjectSettings(_sessionState, _projectAppSettings);
            _widgetRegistry = await _widgetRegistryStore.LoadOrCreateAsync(_activeCustomizationProfile.Placements, CancellationToken.None);
            RenderMainCommandGrid(_activeCustomizationProfile);
            RenderRemoteControlLayer(_activeCustomizationProfile);
            ShowRemoteControlLayer(recenter: true);
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
        _currentManifest = manifest;
        var items = await CreateDocumentListItemsAsync(manifest.Documents);
        BinderList.ItemsSource = items;
        UpdateMainSurface(manifest, items);
        await PushHtmlWorkbenchStateAsync();
    }

    private async Task<IReadOnlyList<DocumentListItem>> CreateDocumentListItemsAsync(IEnumerable<ProjectDocumentInfo> documents)
    {
        var items = new List<DocumentListItem>();
        var metadataByDocumentId = new Dictionary<string, SceneMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in documents)
        {
            var metadata = await _metadataStore.LoadExistingOrDefaultAsync(document.Id, CancellationToken.None);
            metadataByDocumentId[document.Id] = metadata;
            items.Add(DocumentListItem.From(document, metadata));
        }

        _binderMetadataByDocumentId.Clear();
        foreach (var (documentId, metadata) in metadataByDocumentId)
        {
            _binderMetadataByDocumentId[documentId] = metadata;
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

    private void RenderMainCommandGrid(WorkbenchCustomizationProfile profile)
    {
        var placements = new WorkbenchCustomizationResolver(profile)
            .GetPlacements("toolbar", "main");

        MainCommandGrid.Children.Clear();
        MainCommandGrid.Columns = 2;
        MainCommandGrid.Rows = Math.Max(1, (placements.Count + 1) / 2);

        foreach (var placement in placements)
        {
            var command = _commandRegistry.Get(placement.CommandId);
            var button = new System.Windows.Controls.Button
            {
                Content = string.IsNullOrWhiteSpace(placement.Label) ? command.Name : placement.Label,
                Tag = placement.CommandId,
                Margin = new Thickness(0, 0, 10, 10),
                MinHeight = 42,
                Padding = new Thickness(12, 6, 12, 6)
            };
            button.Click += CommandButton_Click;
            MainCommandGrid.Children.Add(button);
        }
    }

    private void RenderRemoteControlLayer(WorkbenchCustomizationProfile profile)
    {
        EnsureRemoteControlLayer().Render(profile, _commandRegistry);
    }

    private RemoteControlLayerWindow EnsureRemoteControlLayer()
    {
        if (_remoteControlLayer is not null)
        {
            return _remoteControlLayer;
        }

        _remoteControlLayer = new RemoteControlLayerWindow();
        _remoteControlLayer.CommandRequested += RemoteControlLayer_CommandRequested;
        _remoteControlLayer.Closed += (_, _) => _remoteControlLayer = null;
        return _remoteControlLayer;
    }

    private async void RemoteControlLayer_CommandRequested(object? sender, string commandId)
    {
        await ExecuteCommandAsync(commandId);
    }

    private Task ShowRemoteControlLayerAsync()
    {
        ShowRemoteControlLayer(recenter: true);
        StatusText.Text = "리모콘 레이어 표시됨";
        return Task.CompletedTask;
    }

    private Task ToggleRemoteControlLayerAsync()
    {
        if (_remoteControlLayer is { IsVisible: true } layer)
        {
            layer.Hide();
            StatusText.Text = "리모콘 꺼짐";
            return Task.CompletedTask;
        }

        ShowRemoteControlLayer(recenter: _remoteControlLayer is null);
        StatusText.Text = "리모콘 켜짐";
        return Task.CompletedTask;
    }

    private void ShowRemoteControlLayer(bool recenter)
    {
        var layer = EnsureRemoteControlLayer();
        layer.Topmost = true;
        if (recenter || double.IsNaN(layer.Left) || double.IsNaN(layer.Top))
        {
            PositionRemoteControlLayer(layer);
        }

        if (!layer.IsVisible)
        {
            layer.Show();
        }

        layer.Topmost = true;
        layer.Activate();
    }

    private bool IsHtmlWorkbenchSurfaceVisible()
    {
        return HtmlWorkbenchSurface.Visibility == Visibility.Visible;
    }

    private void HideNativeRemoteControlLayer()
    {
        if (_remoteControlLayer is { IsVisible: true } layer)
        {
            layer.Hide();
        }
    }

    private void PositionRemoteControlLayer(RemoteControlLayerWindow layer)
    {
        var hostLeft = double.IsNaN(Left) ? 80 : Left;
        var hostTop = double.IsNaN(Top) ? 80 : Top;
        var hostWidth = ActualWidth > 0 ? ActualWidth : Width;
        layer.Left = hostLeft + hostWidth - layer.Width - 18;
        layer.Top = hostTop + 112;
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

    private void BinderListItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item)
        {
            return;
        }

        item.IsSelected = true;
        item.Focus();
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
            await RefreshSceneEntityLinksAsync(document.Id);
            await ShowRequestedSurfaceAsync(startupSurface);
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

    private async Task ShowRequestedSurfaceAsync(string? surface)
    {
        switch (surface)
        {
            case AppSessionState.PreviewSurface:
                await OpenHtmlWorkbenchViewAsync("preview", "미리보기 화면");
                break;
            case AppSessionState.MainSurface:
                await OpenHtmlWorkbenchSurfaceAsync();
                break;
            case AppSessionState.HtmlWorkbenchSurface:
                await OpenHtmlWorkbenchSurfaceAsync();
                break;
            case AppSessionState.RelationshipMapSurface:
                await OpenRelationshipMapAsync();
                break;
            case AppSessionState.EditorSurface:
                await OpenHtmlWorkbenchViewAsync("editor", "작품 수정 화면");
                break;
            default:
                await OpenHtmlWorkbenchViewAsync("editor", "작품 수정 화면");
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
        commandId = AppCommandIds.NormalizeLegacyId(commandId);
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
        await PushHtmlWorkbenchStateAsync();
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
        _commandHandlers[AppCommandIds.StoryRelationshipMapOpen] = OpenRelationshipMapAsync;
        _commandHandlers[AppCommandIds.StoryAddNode] = AddStoryNodeAsync;
        _commandHandlers[AppCommandIds.StoryUpdateNode] = UpdateStoryNodeAsync;
        _commandHandlers[AppCommandIds.StoryDeleteNode] = DeleteStoryNodeAsync;
        _commandHandlers[AppCommandIds.StoryAddRelationship] = AddStoryRelationshipAsync;
        _commandHandlers[AppCommandIds.StoryUpdateRelationship] = UpdateStoryRelationshipAsync;
        _commandHandlers[AppCommandIds.StoryDeleteRelationship] = DeleteStoryRelationshipAsync;
        _commandHandlers[AppCommandIds.SceneEntityLinkAdd] = AddSceneEntityLinkAsync;
        _commandHandlers[AppCommandIds.SceneEntityLinkDelete] = DeleteSceneEntityLinkAsync;
        _commandHandlers[AppCommandIds.DocumentCreateScene] = CreateNewSceneAsync;
        _commandHandlers[AppCommandIds.DocumentCreateStressLarge] = CreateStressDocumentAsync;
        _commandHandlers[AppCommandIds.DocumentDetachCurrent] = DetachWorkbenchAsync;
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
        _commandHandlers[AppCommandIds.RemoteControlShow] = ShowRemoteControlLayerAsync;
        _commandHandlers[AppCommandIds.RemoteControlToggle] = ToggleRemoteControlLayerAsync;
        _commandHandlers[AppCommandIds.RemoteControlOpenSettings] = OpenRemoteControlSettingsAsync;
        _commandHandlers[AppCommandIds.ShortcutsOpenSettings] = OpenShortcutSettingsAsync;
        _commandHandlers[AppCommandIds.ViewEditorOpen] = OpenEditorSurfaceAsync;
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
        _customizationProfiles = new WorkbenchCustomizationProfileService(projectPaths.WorkbenchProfilesPath, _commandRegistry);
        _projectSettingsStore = new ProjectSettingsStore(projectPaths.AppSettingsPath);
        _widgetRegistryStore = new WidgetRegistryStore(projectPaths.WidgetRegistryPath);
        _activeCustomizationProfile = null;
        _projectAppSettings = ProjectAppSettings.Default;
        _widgetRegistry = WorkbenchWidgetRegistry.Empty;
        _currentManifest = null;
        _binderMetadataByDocumentId.Clear();
        _activeDocumentId = "scene-0001";
        _activeDocument = null;
        _activeSceneMetadata = null;
        _editorTextView = DocumentEditorTextView.Empty;
        _htmlActiveView = "editor";
        _lastAppliedPresetSlot = null;
        _dirty = false;
        BinderList.ItemsSource = null;
        SearchResultsList.ItemsSource = null;
        SnapshotList.ItemsSource = null;
        StoryNodeList.ItemsSource = null;
        StoryRelationshipList.ItemsSource = null;
        StoryRelationshipSourceBox.ItemsSource = null;
        StoryRelationshipTargetBox.ItemsSource = null;
        RelationshipEntityList.ItemsSource = null;
        RelationshipList.ItemsSource = null;
        RelationshipSourceBox.ItemsSource = null;
        RelationshipTargetBox.ItemsSource = null;
        SceneEntityLinkList.ItemsSource = null;
        SceneEntityLinkEntityBox.ItemsSource = null;
        RelationshipMapCanvas.Children.Clear();
        TitleBox.Text = "";
        EditorBox.Text = "";
        EditorBox.IsReadOnly = false;
        PreviewText.Text = "";
        ClearStoryStructureInputs();
        ClearSceneInspector();
        ShowHtmlWorkbenchSurface();
        ProjectPathText.Text = _projectRoot;
    }

    private async Task RefreshStoryStructureAsync()
    {
        try
        {
            var entities = await _storyStructureStore.LoadEntitiesAsync(CancellationToken.None);
            var relationships = await _storyStructureStore.LoadRelationshipsAsync(CancellationToken.None);
            var layout = await _storyStructureStore.LoadRelationLayoutAsync(CancellationToken.None);
            var entityItems = entities
                .Select(entity => new StoryEntityListItem(entity))
                .ToList();
            var relationshipItems = relationships
                .Select(relationship => RelationshipListItem.From(relationship, entities))
                .ToList();

            StoryNodeList.ItemsSource = entityItems;
            StoryRelationshipSourceBox.ItemsSource = entityItems;
            StoryRelationshipTargetBox.ItemsSource = entityItems;
            StoryRelationshipList.ItemsSource = relationshipItems;
            RelationshipEntityList.ItemsSource = entityItems;
            RelationshipSourceBox.ItemsSource = entityItems;
            RelationshipTargetBox.ItemsSource = entityItems;
            RelationshipList.ItemsSource = relationshipItems;
            SceneEntityLinkEntityBox.ItemsSource = entityItems;
            RenderRelationshipMap(entities, relationships, layout);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StoryNodeList.ItemsSource = null;
            StoryRelationshipList.ItemsSource = null;
            RelationshipEntityList.ItemsSource = null;
            RelationshipList.ItemsSource = null;
            SceneEntityLinkEntityBox.ItemsSource = null;
            RelationshipMapCanvas.Children.Clear();
            StatusText.Text = $"스토리 구조 로드 실패 - {ex.Message}";
        }
    }

    private async Task AddStoryNodeAsync()
    {
        var name = ReadEntityName();
        if (name.Length == 0)
        {
            StatusText.Text = "캐릭터 이름을 입력하세요.";
            return;
        }

        try
        {
            var entity = await _storyStructureStore.AddEntityAsync(
                ReadEntityType(),
                name,
                ReadEntityRole(),
                ReadEntitySummary(),
                ReadEntityColor(),
                ParseEntityTags(),
                CancellationToken.None);
            StoryNodeNameBox.Text = "";
            StoryNodeSummaryBox.Text = "";
            RelationshipEntityNameBox.Text = "";
            RelationshipEntitySummaryBox.Text = "";
            await RefreshStoryStructureAsync();
            await RefreshSceneEntityLinksAsync(_activeDocumentId);
            SelectEntityInLists(entity.Id);
            StatusText.Text = $"캐릭터 추가됨 {entity.Id} - {entity.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"캐릭터 추가 실패 - {ex.Message}";
        }
    }

    private async Task UpdateStoryNodeAsync()
    {
        var selected = GetSelectedStoryEntity();
        if (selected is null)
        {
            StatusText.Text = "수정할 캐릭터를 선택하세요.";
            return;
        }

        try
        {
            var updated = await _storyStructureStore.UpdateEntityAsync(
                selected.Entity with
                {
                    Type = ReadEntityType(selected.Entity.Type),
                    Name = ReadEntityName(selected.Entity.Name),
                    Role = ReadEntityRole(),
                    Summary = ReadEntitySummary(),
                    Color = ReadEntityColor(selected.Entity.Color),
                    Tags = ParseEntityTags()
                },
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            await RefreshSceneEntityLinksAsync(_activeDocumentId);
            SelectEntityInLists(updated.Id);
            StatusText.Text = $"캐릭터 수정됨 {updated.Id} - {updated.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"캐릭터 수정 실패 - {ex.Message}";
        }
    }

    private async Task DeleteStoryNodeAsync()
    {
        var selected = GetSelectedStoryEntity();
        if (selected is null)
        {
            StatusText.Text = "삭제할 캐릭터를 선택하세요.";
            return;
        }

        if (!ConfirmDeleteStoryEntity(selected.Entity))
        {
            StatusText.Text = $"캐릭터 삭제 취소 {selected.Id} - {selected.Entity.Name}";
            return;
        }

        try
        {
            await _storyStructureStore.DeleteEntityAsync(selected.Id, CancellationToken.None);
            ClearStoryStructureInputs();
            await RefreshStoryStructureAsync();
            await RefreshSceneEntityLinksAsync(_activeDocumentId);
            StatusText.Text = $"캐릭터 삭제됨 {selected.Id} - {selected.Entity.Name}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"캐릭터 삭제 실패 {selected.Id} - {ex.Message}";
        }
    }

    private async Task AddStoryRelationshipAsync()
    {
        var sourceEntityId = ReadRelationshipEndpoint(RelationshipSourceBox, StoryRelationshipSourceBox);
        var targetEntityId = ReadRelationshipEndpoint(RelationshipTargetBox, StoryRelationshipTargetBox);
        if (sourceEntityId.Length == 0 || targetEntityId.Length == 0)
        {
            StatusText.Text = "관계의 시작/도착 캐릭터를 선택하세요.";
            return;
        }

        try
        {
            var relationship = await _storyStructureStore.AddRelationshipAsync(
                sourceEntityId,
                targetEntityId,
                ReadRelationshipLabel(),
                ReadRelationshipNotes(),
                RelationshipDirectionalBox.IsChecked == true,
                CancellationToken.None);
            StoryRelationshipSummaryBox.Text = "";
            RelationshipNotesBox.Text = "";
            await RefreshStoryStructureAsync();
            SelectRelationshipInLists(relationship.Id);
            StatusText.Text = $"관계 추가됨 {relationship.Id} - {relationship.SourceEntityId} -> {relationship.TargetEntityId}";
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

    private async Task UpdateStoryRelationshipAsync()
    {
        var selected = GetSelectedStoryRelationship();
        if (selected is null)
        {
            StatusText.Text = "수정할 관계를 선택하세요.";
            return;
        }

        var sourceEntityId = ReadRelationshipEndpoint(RelationshipSourceBox, StoryRelationshipSourceBox);
        var targetEntityId = ReadRelationshipEndpoint(RelationshipTargetBox, StoryRelationshipTargetBox);
        try
        {
            var updated = await _storyStructureStore.UpdateRelationshipAsync(
                selected.Relationship with
                {
                    SourceEntityId = sourceEntityId,
                    TargetEntityId = targetEntityId,
                    Label = ReadRelationshipLabel(selected.Relationship.Label),
                    Notes = ReadRelationshipNotes(),
                    IsDirectional = RelationshipDirectionalBox.IsChecked == true
                },
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            SelectRelationshipInLists(updated.Id);
            StatusText.Text = $"관계 수정됨 {updated.Id}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            StatusText.Text = $"관계 수정 실패 - {ex.Message}";
        }
    }

    private async Task DeleteStoryRelationshipAsync()
    {
        var selected = GetSelectedStoryRelationship();
        if (selected is null)
        {
            StatusText.Text = "삭제할 관계를 선택하세요.";
            return;
        }

        if (!ConfirmDeleteStoryRelationship(selected.Relationship))
        {
            StatusText.Text = $"관계 삭제 취소 {selected.Id}";
            return;
        }

        try
        {
            await _storyStructureStore.DeleteRelationshipAsync(selected.Id, CancellationToken.None);
            RelationshipLabelBox.Text = "";
            RelationshipNotesBox.Text = "";
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계 삭제됨 {selected.Id}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계 삭제 실패 {selected.Id} - {ex.Message}";
        }
    }

    private void StoryNodeList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (StoryNodeList.SelectedItem is not StoryEntityListItem item)
        {
            return;
        }

        StoryRelationshipSourceBox.SelectedItem ??= item;
        PopulateEntityEditor(item.Entity);
    }

    private void RelationshipEntityList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RelationshipEntityList.SelectedItem is not StoryEntityListItem item)
        {
            return;
        }

        RelationshipSourceBox.SelectedItem ??= item;
        PopulateEntityEditor(item.Entity);
    }

    private void RelationshipList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RelationshipList.SelectedItem is not RelationshipListItem item)
        {
            return;
        }

        PopulateRelationshipEditor(item.Relationship);
    }

    private void ClearStoryStructureInputs()
    {
        StoryNodeNameBox.Text = "";
        StoryNodeSummaryBox.Text = "";
        StoryNodeKindBox.Text = StoryEntityType.Character.ToString();
        RelationshipEntityNameBox.Text = "";
        RelationshipEntityRoleBox.Text = "";
        RelationshipEntitySummaryBox.Text = "";
        RelationshipEntityColorBox.Text = "#2563EB";
        RelationshipEntityTagsBox.Text = "";
        RelationshipEntityTypeBox.Text = StoryEntityType.Character.ToString();
        StoryRelationshipKindBox.Text = "관계";
        StoryRelationshipSummaryBox.Text = "";
        RelationshipLabelBox.Text = "";
        RelationshipNotesBox.Text = "";
        RelationshipDirectionalBox.IsChecked = false;
    }

    private StoryEntityType ReadEntityType(StoryEntityType fallback = StoryEntityType.Character)
    {
        var text = RelationshipEntityTypeBox.Text.Trim();
        if (text.Length == 0)
        {
            text = StoryNodeKindBox.Text.Trim();
        }

        return Enum.TryParse<StoryEntityType>(text, ignoreCase: true, out var type)
            ? type
            : fallback;
    }

    private string ReadEntityName(string fallback = "")
    {
        var text = RelationshipEntityNameBox.Text.Trim();
        if (text.Length == 0)
        {
            text = StoryNodeNameBox.Text.Trim();
        }

        return text.Length == 0 ? fallback : text;
    }

    private string ReadEntityRole()
    {
        return RelationshipEntityRoleBox.Text.Trim();
    }

    private string ReadEntitySummary()
    {
        var text = RelationshipEntitySummaryBox.Text.Trim();
        return text.Length == 0 ? StoryNodeSummaryBox.Text.Trim() : text;
    }

    private string ReadEntityColor(string fallback = "#2563EB")
    {
        var text = RelationshipEntityColorBox.Text.Trim();
        return text.Length == 0 ? fallback : text;
    }

    private IReadOnlyList<string> ParseEntityTags()
    {
        return RelationshipEntityTagsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string ReadRelationshipLabel(string fallback = "Relationship")
    {
        var text = RelationshipLabelBox.Text.Trim();
        if (text.Length == 0)
        {
            text = StoryRelationshipKindBox.Text.Trim();
        }

        return text.Length == 0 ? fallback : text;
    }

    private string ReadRelationshipNotes()
    {
        var text = RelationshipNotesBox.Text.Trim();
        return text.Length == 0 ? StoryRelationshipSummaryBox.Text.Trim() : text;
    }

    private static string ReadRelationshipEndpoint(
        System.Windows.Controls.ComboBox primaryComboBox,
        System.Windows.Controls.ComboBox fallbackComboBox)
    {
        if (primaryComboBox.SelectedItem is StoryEntityListItem primary)
        {
            return primary.Id;
        }

        if (!string.IsNullOrWhiteSpace(primaryComboBox.Text))
        {
            return primaryComboBox.Text.Trim();
        }

        return fallbackComboBox.SelectedItem is StoryEntityListItem fallback
            ? fallback.Id
            : fallbackComboBox.Text.Trim();
    }
    private async Task OpenRelationshipMapAsync()
    {
        await RefreshStoryStructureAsync();
        await OpenHtmlWorkbenchViewAsync("relationship-map", "관계도 화면");
    }

    private void RenderRelationshipMap(
        IReadOnlyList<StoryEntity> entities,
        IReadOnlyList<StoryRelationship> relationships,
        IReadOnlyList<StoryMapNodeLayout> layout)
    {
        RelationshipMapCanvas.Children.Clear();
        var positions = CreateRelationshipMapPositions(entities, layout);
        foreach (var relationship in relationships)
        {
            if (!positions.TryGetValue(relationship.SourceEntityId, out var source) ||
                !positions.TryGetValue(relationship.TargetEntityId, out var target))
            {
                continue;
            }

            RelationshipMapCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = source.X + 65,
                Y1 = source.Y + 26,
                X2 = target.X + 65,
                Y2 = target.Y + 26,
                Stroke = CreateBrush("#64748B"),
                StrokeThickness = relationship.IsDirectional ? 2.5 : 1.5
            });

            var label = new TextBlock
            {
                Text = relationship.Label,
                Background = CreateBrush("#FFFFFF"),
                Foreground = CreateBrush("#111827"),
                Padding = new Thickness(4, 1, 4, 1),
                FontSize = 11
            };
            Canvas.SetLeft(label, (source.X + target.X) / 2 + 52);
            Canvas.SetTop(label, (source.Y + target.Y) / 2 + 16);
            RelationshipMapCanvas.Children.Add(label);
        }

        foreach (var entity in entities)
        {
            var node = CreateRelationshipMapNode(entity);
            Canvas.SetLeft(node, positions[entity.Id].X);
            Canvas.SetTop(node, positions[entity.Id].Y);
            RelationshipMapCanvas.Children.Add(node);
        }
    }

    private static Dictionary<string, System.Windows.Point> CreateRelationshipMapPositions(
        IReadOnlyList<StoryEntity> entities,
        IReadOnlyList<StoryMapNodeLayout> layout)
    {
        var saved = layout.ToDictionary(node => node.EntityId, node => new System.Windows.Point(node.X, node.Y), StringComparer.OrdinalIgnoreCase);
        var positions = new Dictionary<string, System.Windows.Point>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < entities.Count; index++)
        {
            var entity = entities[index];
            positions[entity.Id] = saved.TryGetValue(entity.Id, out var point)
                ? point
                : new System.Windows.Point(44 + (index % 4) * 176, 44 + (index / 4) * 118);
        }

        return positions;
    }

    private Border CreateRelationshipMapNode(StoryEntity entity)
    {
        var stack = new StackPanel { Margin = new Thickness(8, 6, 8, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = entity.Name,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateBrush("#FFFFFF"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{entity.Type} | {entity.Role}",
            Foreground = CreateBrush("#E5E7EB"),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var node = new Border
        {
            Width = 130,
            Height = 52,
            CornerRadius = new CornerRadius(6),
            Background = CreateBrushOrFallback(entity.Color, "#2563EB"),
            BorderBrush = CreateBrush("#111827"),
            BorderThickness = new Thickness(1),
            Child = stack,
            Tag = entity.Id,
            Cursor = System.Windows.Input.Cursors.SizeAll,
            ToolTip = $"{entity.Name}\n{entity.Summary}"
        };
        node.MouseLeftButtonDown += RelationshipMapNode_MouseLeftButtonDown;
        node.MouseMove += RelationshipMapNode_MouseMove;
        node.MouseLeftButtonUp += RelationshipMapNode_MouseLeftButtonUp;
        return node;
    }

    private void RelationshipMapNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border node || node.Tag is not string entityId)
        {
            return;
        }

        _draggedRelationshipMapEntityId = entityId;
        _relationshipMapDragOffset = e.GetPosition(node);
        node.CaptureMouse();
        e.Handled = true;
    }

    private void RelationshipMapNode_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_draggedRelationshipMapEntityId is null ||
            sender is not Border node ||
            e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var point = e.GetPosition(RelationshipMapCanvas);
        Canvas.SetLeft(node, Math.Max(0, point.X - _relationshipMapDragOffset.X));
        Canvas.SetTop(node, Math.Max(0, point.Y - _relationshipMapDragOffset.Y));
    }

    private async void RelationshipMapNode_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedRelationshipMapEntityId is null || sender is not Border node)
        {
            return;
        }

        var entityId = _draggedRelationshipMapEntityId;
        _draggedRelationshipMapEntityId = null;
        node.ReleaseMouseCapture();
        var x = Canvas.GetLeft(node);
        var y = Canvas.GetTop(node);
        try
        {
            await _storyStructureStore.SaveNodeLayoutAsync(entityId, x, y, CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 위치 저장됨 {entityId} - X {x:N0}, Y {y:N0}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계도 위치 저장 실패 {entityId} - {ex.Message}";
        }
    }

    private StoryEntityListItem? GetSelectedStoryEntity()
    {
        return RelationshipEntityList.SelectedItem as StoryEntityListItem
            ?? StoryNodeList.SelectedItem as StoryEntityListItem;
    }

    private RelationshipListItem? GetSelectedStoryRelationship()
    {
        return RelationshipList.SelectedItem as RelationshipListItem
            ?? StoryRelationshipList.SelectedItem as RelationshipListItem;
    }

    private void PopulateEntityEditor(StoryEntity entity)
    {
        RelationshipEntityNameBox.Text = entity.Name;
        RelationshipEntityTypeBox.Text = entity.Type.ToString();
        RelationshipEntityRoleBox.Text = entity.Role;
        RelationshipEntitySummaryBox.Text = entity.Summary;
        RelationshipEntityColorBox.Text = entity.Color;
        RelationshipEntityTagsBox.Text = string.Join(", ", entity.Tags);
        StoryNodeNameBox.Text = entity.Name;
        StoryNodeKindBox.Text = entity.Type.ToString();
        StoryNodeSummaryBox.Text = entity.Summary;
    }

    private void PopulateRelationshipEditor(StoryRelationship relationship)
    {
        SelectComboBoxItem(RelationshipSourceBox, relationship.SourceEntityId);
        SelectComboBoxItem(RelationshipTargetBox, relationship.TargetEntityId);
        SelectComboBoxItem(StoryRelationshipSourceBox, relationship.SourceEntityId);
        SelectComboBoxItem(StoryRelationshipTargetBox, relationship.TargetEntityId);
        RelationshipLabelBox.Text = relationship.Label;
        StoryRelationshipKindBox.Text = relationship.Label;
        RelationshipNotesBox.Text = relationship.Notes;
        StoryRelationshipSummaryBox.Text = relationship.Notes;
        RelationshipDirectionalBox.IsChecked = relationship.IsDirectional;
    }

    private void SelectEntityInLists(string entityId)
    {
        SelectListBoxItem(StoryNodeList, entityId);
        SelectListBoxItem(RelationshipEntityList, entityId);
    }

    private void SelectRelationshipInLists(string relationshipId)
    {
        SelectListBoxItem(StoryRelationshipList, relationshipId);
        SelectListBoxItem(RelationshipList, relationshipId);
    }

    private static void SelectListBoxItem(System.Windows.Controls.ListBox listBox, string id)
    {
        foreach (var item in listBox.Items)
        {
            if (item is StoryEntityListItem entity && string.Equals(entity.Id, id, StringComparison.OrdinalIgnoreCase) ||
                item is RelationshipListItem relationship && string.Equals(relationship.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                listBox.SelectedItem = item;
                return;
            }
        }
    }

    private static void SelectComboBoxItem(System.Windows.Controls.ComboBox comboBox, string entityId)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is StoryEntityListItem entity && string.Equals(entity.Id, entityId, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.Text = entityId;
    }

    private bool ConfirmDeleteStoryEntity(StoryEntity entity)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"캐릭터를 삭제할까요?\n관련 관계와 관계도 위치도 함께 삭제됩니다.\n\n{entity.Id} - {entity.Name}",
            "캐릭터 삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private bool ConfirmDeleteStoryRelationship(StoryRelationship relationship)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            $"관계를 삭제할까요?\n\n{relationship.Id} - {relationship.Label}",
            "관계 삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private static SolidColorBrush CreateBrushOrFallback(string color, string fallback)
    {
        try
        {
            return CreateBrush(color);
        }
        catch (FormatException)
        {
            return CreateBrush(fallback);
        }
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

    private async Task AddSceneEntityLinkAsync()
    {
        if (_activeDocument is null)
        {
            StatusText.Text = "연결할 활성 장면이 없습니다.";
            return;
        }

        var entityId = ReadSceneEntityLinkEntityId();
        if (entityId.Length == 0)
        {
            StatusText.Text = "장면에 연결할 인물/설정 항목을 선택하세요.";
            return;
        }

        try
        {
            var link = await _sceneEntityLinkStore.AddOrUpdateAsync(
                _activeDocument.Id,
                entityId,
                SceneEntityLinkRoleBox.Text,
                SceneEntityLinkNotesBox.Text,
                CancellationToken.None);
            await RefreshSceneEntityLinksAsync(_activeDocument.Id);
            SelectSceneEntityLink(link.SceneId, link.EntityId);
            StatusText.Text = $"장면 연결 저장됨 {link.SceneId} - {link.EntityId} ({link.Role})";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            StatusText.Text = $"장면 연결 저장 실패 {_activeDocument.Id} - {ex.Message}";
        }
    }

    private async Task DeleteSceneEntityLinkAsync()
    {
        var selected = GetSelectedSceneEntityLink();
        if (selected is null)
        {
            StatusText.Text = "해제할 장면 연결을 선택하세요.";
            return;
        }

        try
        {
            await _sceneEntityLinkStore.DeleteAsync(selected.Link.SceneId, selected.Link.EntityId, CancellationToken.None);
            await RefreshSceneEntityLinksAsync(selected.Link.SceneId);
            SceneEntityLinkNotesBox.Text = "";
            StatusText.Text = $"장면 연결 해제됨 {selected.Link.SceneId} - {selected.Link.EntityId}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"장면 연결 해제 실패 {selected.Link.SceneId} - {ex.Message}";
        }
    }

    private async Task RefreshSceneEntityLinksAsync(string documentId)
    {
        try
        {
            var entities = await _storyStructureStore.LoadEntitiesAsync(CancellationToken.None);
            var links = await _sceneEntityLinkStore.LoadForSceneAsync(documentId, CancellationToken.None);
            SceneEntityLinkList.ItemsSource = links
                .Select(link => SceneEntityLinkListItem.From(link, entities))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SceneEntityLinkList.ItemsSource = null;
            StatusText.Text = $"장면 연결 목록 불러오기 실패 {documentId} - {ex.Message}";
        }
    }

    private void SceneEntityLinkList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SceneEntityLinkList.SelectedItem is not SceneEntityLinkListItem item)
        {
            return;
        }

        foreach (var entityItem in SceneEntityLinkEntityBox.ItemsSource?.OfType<StoryEntityListItem>() ?? [])
        {
            if (string.Equals(entityItem.Id, item.Link.EntityId, StringComparison.OrdinalIgnoreCase))
            {
                SceneEntityLinkEntityBox.SelectedItem = entityItem;
                break;
            }
        }

        SceneEntityLinkRoleBox.Text = item.Link.Role;
        SceneEntityLinkNotesBox.Text = item.Link.Notes;
    }

    private string ReadSceneEntityLinkEntityId()
    {
        if (SceneEntityLinkEntityBox.SelectedItem is StoryEntityListItem selected)
        {
            return selected.Id;
        }

        return SceneEntityLinkEntityBox.Text.Trim();
    }

    private SceneEntityLinkListItem? GetSelectedSceneEntityLink()
    {
        return SceneEntityLinkList.SelectedItem is SceneEntityLinkListItem item
            ? item
            : null;
    }

    private void SelectSceneEntityLink(string sceneId, string entityId)
    {
        foreach (var item in SceneEntityLinkList.ItemsSource?.OfType<SceneEntityLinkListItem>() ?? [])
        {
            if (string.Equals(item.Link.SceneId, sceneId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Link.EntityId, entityId, StringComparison.OrdinalIgnoreCase))
            {
                SceneEntityLinkList.SelectedItem = item;
                break;
            }
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

    private Task DetachWorkbenchAsync()
    {
        var window = new WorkbenchDetachedWindow(
            _surfaceClaims,
            $"detached-{Guid.NewGuid():N}",
            _storyStructureStore,
            CreateHtmlWorkbenchPayloadAsync,
            ProcessHtmlWorkbenchMessageAsync)
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = Left + 48,
            Top = Top + 48,
            Topmost = true,
            ShowActivated = true
        };

        _detachedWorkbenchWindows.Add(window);
        window.SurfaceSelectionChanged += (_, _) => RefreshDetachedSurfaceAvailability();
        window.Closed += (_, _) =>
        {
            _detachedWorkbenchWindows.Remove(window);
            RefreshDetachedSurfaceAvailability();
        };
        window.Show();
        window.Activate();
        StatusText.Text = "분리 작업대 열림 - 화면을 선택하세요";
        return Task.CompletedTask;
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
            Top = Top + 48,
            Topmost = true,
            ShowActivated = true
        };

        window.Show();
        window.Activate();
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

    private async Task OpenRemoteControlSettingsAsync()
    {
        _activeCustomizationProfile ??= await _customizationProfiles.LoadOrCreateActiveProfileAsync(CancellationToken.None);
        await OpenHtmlWorkbenchViewAsync("remote-settings", "리모컨 편집 화면");
    }

    private async Task OpenShortcutSettingsAsync()
    {
        await OpenHtmlWorkbenchViewAsync("shortcuts", "단축키 설정 화면");
    }

    private Task OpenHelpWindowAsync()
    {
        return OpenHtmlWorkbenchViewAsync("help", "도움말 화면");
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
            _binderMetadataByDocumentId[documentId] = metadata;
            PopulateSceneInspector(metadata);
            await PushHtmlWorkbenchStateAsync();
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
        SceneEntityLinkList.ItemsSource = null;
        SceneEntityLinkEntityBox.SelectedItem = null;
        SceneEntityLinkRoleBox.Text = "등장";
        SceneEntityLinkNotesBox.Text = "";
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
        await OpenHtmlWorkbenchViewAsync("editor", "메인 화면");
    }

    private async Task OpenEditorSurfaceAsync()
    {
        await OpenHtmlWorkbenchViewAsync("editor", "작품 수정 화면");
    }

    private async Task<bool> OpenHtmlWorkbenchSurfaceAsync(string claimedSurfaceId = AppSessionState.HtmlWorkbenchSurface)
    {
        if (!ShowHtmlWorkbenchSurfaceForClaim(claimedSurfaceId))
        {
            return false;
        }

        if (IsLoaded)
        {
            _ = InitializeHtmlWorkbenchSurfaceAsync();
        }

        await PersistSessionStateAsync();
        return true;
    }

    private async Task OpenHtmlWorkbenchViewAsync(string activeView, string status)
    {
        var normalizedView = NormalizeHtmlActiveView(activeView);
        if (!await OpenHtmlWorkbenchSurfaceAsync(GetSurfaceIdForHtmlActiveView(normalizedView)))
        {
            return;
        }

        _htmlActiveView = normalizedView;
        StatusText.Text = status;
        await PushHtmlWorkbenchStateAsync();
    }

    private static string NormalizeHtmlActiveView(string? activeView)
    {
        var normalized = (activeView ?? "").Trim();
        return normalized is
            "editor" or
            "preview" or
            "relationship-map" or
            "shortcuts" or
            "remote-settings" or
            "help"
            ? normalized
            : "editor";
    }

    private static string GetSurfaceIdForHtmlActiveView(string activeView)
    {
        return activeView switch
        {
            "editor" => AppSessionState.EditorSurface,
            "preview" => AppSessionState.PreviewSurface,
            "relationship-map" => AppSessionState.RelationshipMapSurface,
            _ => AppSessionState.HtmlWorkbenchSurface
        };
    }

    private async Task InitializeHtmlWorkbenchSurfaceAsync()
    {
        try
        {
            await EnsureHtmlWorkbenchAsync();
            await PushHtmlWorkbenchStateAsync();
        }
        catch (InvalidOperationException ex)
        {
            StatusText.Text = $"메인 초기화 실패: {ex.Message}";
        }
        catch (IOException ex)
        {
            StatusText.Text = $"메인 파일 오류: {ex.Message}";
        }
    }

    private async Task EnsureHtmlWorkbenchAsync()
    {
        if (_htmlWorkbenchInitialized)
        {
            return;
        }

        var indexPath = Path.Combine(AppContext.BaseDirectory, "WebWorkbench", "index.html");
        if (!File.Exists(indexPath))
        {
            StatusText.Text = $"메인 파일 없음: {indexPath}";
            return;
        }

        await HtmlWorkbenchBrowser.EnsureCoreWebView2Async();
        HtmlWorkbenchBrowser.CoreWebView2.WebMessageReceived += HtmlWorkbenchBrowser_WebMessageReceived;
        HtmlWorkbenchBrowser.NavigationCompleted += async (_, _) => await PushHtmlWorkbenchStateAsync();
        HtmlWorkbenchBrowser.Source = new Uri(indexPath);
        _htmlWorkbenchInitialized = true;
    }

    private async void HtmlWorkbenchBrowser_WebMessageReceived(
        object? sender,
        Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        await ProcessHtmlWorkbenchMessageAsync(e.WebMessageAsJson);
    }

    private async Task ProcessHtmlWorkbenchMessageAsync(string webMessageAsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(webMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var type))
            {
                return;
            }

            var messageType = type.GetString();
            if (string.Equals(messageType, "command", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("commandId", out var commandIdElement))
                {
                    var commandId = commandIdElement.GetString();
                    if (!string.IsNullOrWhiteSpace(commandId))
                    {
                        await ExecuteCommandAsync(commandId);
                    }
                }

                return;
            }

            if (string.Equals(messageType, "activeScene.update", StringComparison.OrdinalIgnoreCase))
            {
                var title = root.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString() ?? ""
                    : "";
                var editorText = root.TryGetProperty("editorText", out var editorTextElement)
                    ? editorTextElement.GetString() ?? ""
                    : "";
                ApplyHtmlActiveSceneUpdate(title, editorText);
                return;
            }

            if (string.Equals(messageType, "document.select", StringComparison.OrdinalIgnoreCase))
            {
                var documentId = root.TryGetProperty("documentId", out var documentIdElement)
                    ? documentIdElement.GetString() ?? ""
                    : "";
                if (!string.IsNullOrWhiteSpace(documentId))
                {
                    await SelectDocumentAsync(documentId);
                }

                return;
            }

            if (string.Equals(messageType, "document.command", StringComparison.OrdinalIgnoreCase))
            {
                var documentId = root.TryGetProperty("documentId", out var documentIdElement)
                    ? documentIdElement.GetString() ?? ""
                    : "";
                var commandId = root.TryGetProperty("commandId", out var commandIdElement)
                    ? commandIdElement.GetString() ?? ""
                    : "";
                await ApplyHtmlBinderCommandAsync(documentId, commandId);
                return;
            }

            if (string.Equals(messageType, "story.entity.add", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryEntityAddAsync(
                    ReadJsonString(root, "name"),
                    ReadJsonString(root, "role"));
                return;
            }

            if (string.Equals(messageType, "story.relationship.add", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryRelationshipAddAsync(
                    ReadJsonString(root, "sourceEntityId"),
                    ReadJsonString(root, "targetEntityId"),
                    ReadJsonString(root, "label"),
                    ReadJsonString(root, "notes"));
                return;
            }

            if (string.Equals(messageType, "story.entity.update", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryEntityUpdateAsync(
                    ReadJsonString(root, "entityId"),
                    ReadJsonString(root, "name"),
                    ReadJsonString(root, "role"));
                return;
            }

            if (string.Equals(messageType, "story.entity.delete", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryEntityDeleteAsync(ReadJsonString(root, "entityId"));
                return;
            }

            if (string.Equals(messageType, "story.relationship.update", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryRelationshipUpdateAsync(
                    ReadJsonString(root, "relationshipId"),
                    ReadJsonString(root, "sourceEntityId"),
                    ReadJsonString(root, "targetEntityId"),
                    ReadJsonString(root, "label"),
                    ReadJsonString(root, "notes"));
                return;
            }

            if (string.Equals(messageType, "story.relationship.delete", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryRelationshipDeleteAsync(ReadJsonString(root, "relationshipId"));
                return;
            }

            if (string.Equals(messageType, "story.layout.update", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlStoryLayoutUpdateAsync(
                    ReadJsonString(root, "entityId"),
                    ReadJsonDouble(root, "x"),
                    ReadJsonDouble(root, "y"));
                return;
            }

            if (string.Equals(messageType, "trash.restore", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlTrashRestoreAsync(ReadJsonString(root, "trashId"));
                return;
            }

            if (string.Equals(messageType, "shortcut.update", StringComparison.OrdinalIgnoreCase))
            {
                await ApplyHtmlShortcutUpdateAsync(
                    ReadJsonString(root, "commandId"),
                    ReadJsonString(root, "scope"),
                    ReadJsonString(root, "gesture"));
                return;
            }

            if (string.Equals(messageType, "remoteSettings.update", StringComparison.OrdinalIgnoreCase))
            {
                var commandIds = root.TryGetProperty("commandIds", out var commandIdsElement) &&
                                 commandIdsElement.ValueKind == JsonValueKind.Array
                    ? commandIdsElement.EnumerateArray()
                        .Select(item => item.GetString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item!)
                        .ToList()
                    : [];
                await ApplyHtmlRemoteSettingsUpdateAsync(commandIds);
            }
        }
        catch (JsonException ex)
        {
            StatusText.Text = $"메인 메시지 오류: {ex.Message}";
        }
    }

    private static string ReadJsonString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? ""
            : "";
    }

    private static double ReadJsonDouble(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element) && element.TryGetDouble(out var value)
            ? value
            : 0;
    }

    private void ApplyHtmlActiveSceneUpdate(string title, string editorText)
    {
        if (_activeDocument is null || string.IsNullOrWhiteSpace(_activeDocumentId))
        {
            return;
        }

        var document = DocumentEditorTextService.UpdateFromEditorText(
            _activeDocument,
            title,
            editorText,
            _editorTextView);
        _activeDocument = document;
        _editorTextView = _editorTextView with
        {
            Text = editorText,
            VisibleParagraphCount = DocumentEditorTextService.CountEditorParagraphs(editorText)
        };

        _loadingDocument = true;
        try
        {
            TitleBox.Text = document.Title;
            EditorBox.Text = editorText;
        }
        finally
        {
            _loadingDocument = false;
        }

        UpdateMetrics(document);
        _dirty = true;
        _lastEditAt = DateTimeOffset.Now;
        StatusText.Text = $"HTML 편집 반영됨 {document.Id}";
    }

    private async Task ApplyHtmlBinderCommandAsync(string documentId, string commandId)
    {
        commandId = AppCommandIds.NormalizeLegacyId(commandId);
        if (string.IsNullOrWhiteSpace(commandId))
        {
            StatusText.Text = "바인더 명령 실패 - 명령이 없습니다.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(documentId))
        {
            SelectBinderItem(documentId);
            if (CommandRequiresActiveDocument(commandId) &&
                !string.Equals(documentId, _activeDocumentId, StringComparison.OrdinalIgnoreCase))
            {
                await SelectDocumentAsync(documentId);
            }
        }

        await ExecuteCommandAsync(commandId);
    }

    private async Task ApplyHtmlStoryEntityAddAsync(string name, string role)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0)
        {
            StatusText.Text = "관계도 캐릭터 추가 실패 - 이름을 입력하세요.";
            return;
        }

        try
        {
            var entity = await _storyStructureStore.AddEntityAsync(
                StoryEntityType.Character,
                normalizedName,
                role.Trim(),
                "",
                "#2563EB",
                [],
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 캐릭터 추가됨 {entity.Id} - {entity.Name}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"관계도 캐릭터 추가 실패 - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryRelationshipAddAsync(
        string sourceEntityId,
        string targetEntityId,
        string label,
        string notes)
    {
        if (string.IsNullOrWhiteSpace(sourceEntityId) || string.IsNullOrWhiteSpace(targetEntityId))
        {
            StatusText.Text = "관계도 관계 추가 실패 - 시작/도착 캐릭터를 선택하세요.";
            return;
        }

        try
        {
            var relationship = await _storyStructureStore.AddRelationshipAsync(
                sourceEntityId.Trim(),
                targetEntityId.Trim(),
                string.IsNullOrWhiteSpace(label) ? "관계" : label.Trim(),
                notes.Trim(),
                true,
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 관계 추가됨 {relationship.Id} - {relationship.Label}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            StatusText.Text = $"관계도 관계 추가 실패 - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryEntityUpdateAsync(string entityId, string name, string role)
    {
        if (string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(name))
        {
            StatusText.Text = "관계도 캐릭터 수정 실패 - 대상과 이름을 확인하세요.";
            return;
        }

        try
        {
            var entities = await _storyStructureStore.LoadEntitiesAsync(CancellationToken.None);
            var existing = entities.FirstOrDefault(entity =>
                string.Equals(entity.Id, entityId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                throw new KeyNotFoundException($"Story entity not found: {entityId}");
            }

            var updated = await _storyStructureStore.UpdateEntityAsync(
                existing with
                {
                    Name = name.Trim(),
                    Role = role.Trim()
                },
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 캐릭터 수정됨 {updated.Id} - {updated.Name}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계도 캐릭터 수정 실패 {entityId} - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryEntityDeleteAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            StatusText.Text = "관계도 캐릭터 삭제 실패 - 대상이 없습니다.";
            return;
        }

        try
        {
            await _storyStructureStore.DeleteEntityAsync(entityId.Trim(), CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 캐릭터 삭제됨 {entityId.Trim()}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계도 캐릭터 삭제 실패 {entityId} - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryRelationshipUpdateAsync(
        string relationshipId,
        string sourceEntityId,
        string targetEntityId,
        string label,
        string notes)
    {
        if (string.IsNullOrWhiteSpace(relationshipId) ||
            string.IsNullOrWhiteSpace(sourceEntityId) ||
            string.IsNullOrWhiteSpace(targetEntityId))
        {
            StatusText.Text = "관계도 관계 수정 실패 - 관계와 캐릭터를 확인하세요.";
            return;
        }

        try
        {
            var relationships = await _storyStructureStore.LoadRelationshipsAsync(CancellationToken.None);
            var existing = relationships.FirstOrDefault(relationship =>
                string.Equals(relationship.Id, relationshipId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                throw new KeyNotFoundException($"Story relationship not found: {relationshipId}");
            }

            var updated = await _storyStructureStore.UpdateRelationshipAsync(
                existing with
                {
                    SourceEntityId = sourceEntityId.Trim(),
                    TargetEntityId = targetEntityId.Trim(),
                    Label = string.IsNullOrWhiteSpace(label) ? "관계" : label.Trim(),
                    Notes = notes.Trim()
                },
                CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 관계 수정됨 {updated.Id} - {updated.Label}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            StatusText.Text = $"관계도 관계 수정 실패 {relationshipId} - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryRelationshipDeleteAsync(string relationshipId)
    {
        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            StatusText.Text = "관계도 관계 삭제 실패 - 대상이 없습니다.";
            return;
        }

        try
        {
            await _storyStructureStore.DeleteRelationshipAsync(relationshipId.Trim(), CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 관계 삭제됨 {relationshipId.Trim()}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계도 관계 삭제 실패 {relationshipId} - {ex.Message}";
        }
    }

    private async Task ApplyHtmlStoryLayoutUpdateAsync(string entityId, double x, double y)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            StatusText.Text = "관계도 위치 저장 실패 - 캐릭터가 없습니다.";
            return;
        }

        try
        {
            await _storyStructureStore.SaveNodeLayoutAsync(entityId.Trim(), x, y, CancellationToken.None);
            await RefreshStoryStructureAsync();
            StatusText.Text = $"관계도 위치 저장됨 {entityId} - X {x:N0}, Y {y:N0}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"관계도 위치 저장 실패 {entityId} - {ex.Message}";
        }
    }

    private static bool CommandRequiresActiveDocument(string commandId)
    {
        return commandId is
            AppCommandIds.ProjectSave or
            AppCommandIds.ExportCurrentScene or
            AppCommandIds.SnapshotCreateCurrent or
            AppCommandIds.DocumentDetachCurrent;
    }

    private async Task ApplyHtmlRemoteSettingsUpdateAsync(IReadOnlyList<string> commandIds)
    {
        _activeCustomizationProfile ??= await _customizationProfiles.LoadOrCreateActiveProfileAsync(CancellationToken.None);
        var rows = new List<RemoteControlSettingsRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 1;
        foreach (var requestedCommandId in commandIds)
        {
            var commandId = AppCommandIds.NormalizeLegacyId(requestedCommandId);
            if (string.IsNullOrWhiteSpace(commandId) || !seen.Add(commandId))
            {
                continue;
            }

            AppCommand command;
            try
            {
                command = _commandRegistry.Get(commandId);
            }
            catch (KeyNotFoundException)
            {
                continue;
            }

            rows.Add(new RemoteControlSettingsRow(
                command.Id,
                command.Name,
                command.Category,
                true,
                order++,
                command.Name));
        }

        _activeCustomizationProfile = RemoteControlSettingsWindow.ApplyRemoteRows(
            _activeCustomizationProfile,
            rows,
            _commandRegistry,
            DateTimeOffset.UtcNow);
        await _customizationProfiles.SaveProfileAsync(_activeCustomizationProfile, CancellationToken.None);
        RenderRemoteControlLayer(_activeCustomizationProfile);
        StatusText.Text = $"리모컨 바로가기 {rows.Count:N0}개 저장됨";
        await PushHtmlWorkbenchStateAsync();
    }

    private async Task ApplyHtmlTrashRestoreAsync(string trashId)
    {
        if (string.IsNullOrWhiteSpace(trashId))
        {
            StatusText.Text = "휴지통 복원 실패 - 대상이 없습니다.";
            return;
        }

        try
        {
            var restored = await _store.RestoreTrashedDocumentAsync(trashId.Trim(), CancellationToken.None);
            var manifest = await _store.LoadManifestAsync(CancellationToken.None);
            await RefreshBinderAsync(manifest);
            await SelectDocumentAsync(restored.Id);
            StatusText.Text = $"휴지통 복원됨 {restored.Id} - {restored.Title}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            StatusText.Text = $"휴지통 복원 실패 {trashId} - {ex.Message}";
        }
    }

    private async Task ApplyHtmlShortcutUpdateAsync(string commandId, string scope, string gesture)
    {
        commandId = AppCommandIds.NormalizeLegacyId(commandId);
        if (string.IsNullOrWhiteSpace(commandId) || string.IsNullOrWhiteSpace(gesture))
        {
            StatusText.Text = "단축키 저장 실패 - 명령과 단축키를 확인하세요.";
            return;
        }

        if (!Enum.TryParse<CommandScope>(scope, ignoreCase: true, out var parsedScope))
        {
            parsedScope = CommandScope.Workbench;
        }

        try
        {
            var command = _commandRegistry.Get(commandId);
            var binding = new ShortcutBinding(command.Id, gesture.Trim(), parsedScope);
            if (!_shortcutManager.TryBind(binding, out var conflictCommandId))
            {
                StatusText.Text = $"단축키 충돌 {binding.Gesture} - {conflictCommandId}";
                return;
            }

            await _shortcuts.SaveAsync(_shortcutManager, CancellationToken.None);
            StatusText.Text = $"단축키 저장됨 {command.Name} - {ShortcutManager.NormalizeGesture(gesture)}";
            await PushHtmlWorkbenchStateAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            StatusText.Text = $"단축키 저장 실패 {commandId} - {ex.Message}";
        }
    }

    private async Task PushHtmlWorkbenchStateAsync()
    {
        if (_htmlWorkbenchInitialized && HtmlWorkbenchBrowser.CoreWebView2 is not null)
        {
            var payload = await CreateHtmlWorkbenchPayloadAsync(_htmlActiveView);
            var message = JsonSerializer.Serialize(new
            {
                type = "state",
                payload
            }, WebWorkbenchJsonOptions);

            HtmlWorkbenchBrowser.CoreWebView2.PostWebMessageAsJson(message);
        }

        foreach (var window in _detachedWorkbenchWindows.ToList())
        {
            await window.PushHtmlStateAsync();
        }
    }

    private async Task<WebWorkbenchPayload> CreateHtmlWorkbenchPayloadAsync(string activeView)
    {
        var manifest = _currentManifest ?? new ProjectManifest(
            1,
            Path.GetFileNameWithoutExtension(_projectRoot),
            []);
        var profile = _activeCustomizationProfile ?? WorkbenchCustomizationProfileFactory.CreateDefault(
            "profile-html-default",
            "메인",
            _commandRegistry);
        var story = await CreateHtmlStoryPayloadAsync();
        var trash = await CreateHtmlTrashPayloadAsync();
        return WebWorkbenchPayloadFactory.Create(
            manifest,
            _projectRoot,
            _activeDocument,
            _activeSceneMetadata,
            _binderMetadataByDocumentId,
            profile,
            _commandRegistry,
            StatusText.Text,
            _graphicPreset.Name,
            _autosaveEnabled,
            _widgetRegistry,
            NormalizeHtmlActiveView(activeView),
            CreateHtmlPreviewText(),
            _shortcutManager.Bindings,
            story,
            trash);
    }

    private async Task<WebWorkbenchStory> CreateHtmlStoryPayloadAsync()
    {
        try
        {
            var entities = await _storyStructureStore.LoadEntitiesAsync(CancellationToken.None);
            var relationships = await _storyStructureStore.LoadRelationshipsAsync(CancellationToken.None);
            var layout = await _storyStructureStore.LoadRelationLayoutAsync(CancellationToken.None);
            var positions = CreateRelationshipMapPositions(entities, layout);
            return new WebWorkbenchStory(
                entities
                    .Select(entity =>
                    {
                        var position = positions.GetValueOrDefault(entity.Id, new System.Windows.Point(0, 0));
                        return new WebWorkbenchStoryEntity(
                            entity.Id,
                            entity.Type.ToString(),
                            entity.Name,
                            entity.Role,
                            entity.Summary,
                            entity.Color,
                            entity.Tags,
                            position.X,
                            position.Y);
                    })
                    .ToList(),
                relationships
                    .Select(relationship => new WebWorkbenchStoryRelationship(
                        relationship.Id,
                        relationship.SourceEntityId,
                        relationship.TargetEntityId,
                        relationship.Label,
                        relationship.Notes,
                        relationship.IsDirectional))
                    .ToList());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"관계도 상태 생성 실패 - {ex.Message}";
            return new WebWorkbenchStory([], []);
        }
    }

    private async Task<IReadOnlyList<WebWorkbenchTrashItem>> CreateHtmlTrashPayloadAsync()
    {
        try
        {
            var trash = await _store.ListTrashedDocumentsAsync(CancellationToken.None);
            return trash
                .Select(item => new WebWorkbenchTrashItem(
                    item.TrashId,
                    item.Id,
                    item.Title,
                    item.DeletedAt))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText.Text = $"휴지통 상태 생성 실패 - {ex.Message}";
            return [];
        }
    }

    private string CreateHtmlPreviewText()
    {
        if (_activeDocument is not null)
        {
            return PreviewTextService.CreatePreview(TextExportService.ToPlainText(_activeDocument));
        }

        return PreviewTextService.CreatePreview(EditorBox.Text);
    }

    private Task TogglePreviewAsync()
    {
        return string.Equals(_htmlActiveView, "preview", StringComparison.OrdinalIgnoreCase)
            ? OpenHtmlWorkbenchViewAsync("editor", "작품 수정 화면")
            : OpenHtmlWorkbenchViewAsync("preview", "미리보기 화면");
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

    private void FocusDurationMinutesBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_focusMode)
        {
            return;
        }

        if (TryReadFocusDurationMinutes(out var minutes))
        {
            _focusDurationMinutes = minutes;
            UpdateFocusButtonIdleContent();
        }
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

            var isAutosave = verb.Contains("자동저장", StringComparison.OrdinalIgnoreCase);
            await Task.Run(async () =>
            {
                if (isAutosave)
                {
                    await _store.SaveAutosaveCopyAsync(document, CancellationToken.None);
                }

                await _store.SaveDocumentAsync(document, CancellationToken.None);
            });
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
        if (!TryClaimMainSurface(AppSessionState.PreviewSurface))
        {
            return;
        }

        ShowNativeWorkbenchChrome();
        var sourceText = _editorTextView.IsSegmentMode && _activeDocument is not null
            ? TextExportService.ToPlainText(_activeDocument)
            : EditorBox.Text;

        PreviewText.Text = PreviewTextService.CreatePreview(sourceText);
        HtmlWorkbenchSurface.Visibility = Visibility.Collapsed;
        MainSurface.Visibility = Visibility.Collapsed;
        RelationshipMapSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Visible;
        PreviewModeButton.Content = "편집";
        RememberSessionState(AppSessionState.PreviewSurface);
        StatusText.Text = "미리보기 렌더링됨";
    }

    private void ShowEditorSurface()
    {
        if (!TryClaimMainSurface(AppSessionState.EditorSurface))
        {
            return;
        }

        ShowNativeWorkbenchChrome();
        HtmlWorkbenchSurface.Visibility = Visibility.Collapsed;
        MainSurface.Visibility = Visibility.Collapsed;
        RelationshipMapSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Visible;
        PreviewModeButton.Content = "미리보기";
        RememberSessionState(AppSessionState.EditorSurface);
        EditorBox.Focus();
    }

    private void ShowMainSurface()
    {
        if (!TryClaimMainSurface(AppSessionState.MainSurface))
        {
            return;
        }

        ShowNativeWorkbenchChrome();
        HtmlWorkbenchSurface.Visibility = Visibility.Collapsed;
        MainSurface.Visibility = Visibility.Visible;
        RelationshipMapSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        PreviewModeButton.Content = "미리보기";
        RememberSessionState(AppSessionState.MainSurface);
        StatusText.Text = "메인 화면";
    }

    private bool ShowHtmlWorkbenchSurface()
    {
        return ShowHtmlWorkbenchSurfaceForClaim(AppSessionState.HtmlWorkbenchSurface);
    }

    private bool ShowHtmlWorkbenchSurfaceForClaim(string claimedSurfaceId)
    {
        if (!TryClaimMainSurface(claimedSurfaceId))
        {
            return false;
        }

        HideNativeWorkbenchChrome();
        HtmlWorkbenchSurface.Visibility = Visibility.Visible;
        MainSurface.Visibility = Visibility.Collapsed;
        RelationshipMapSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        PreviewModeButton.Content = "미리보기";
        RememberSessionState(claimedSurfaceId);
        StatusText.Text = "메인 화면";
        return true;
    }

    private void ShowRelationshipMapSurface()
    {
        if (!TryClaimMainSurface(AppSessionState.RelationshipMapSurface))
        {
            return;
        }

        ShowNativeWorkbenchChrome();
        HtmlWorkbenchSurface.Visibility = Visibility.Collapsed;
        MainSurface.Visibility = Visibility.Collapsed;
        PreviewSurface.Visibility = Visibility.Collapsed;
        EditorSurface.Visibility = Visibility.Collapsed;
        RelationshipMapSurface.Visibility = Visibility.Visible;
        PreviewModeButton.Content = "미리보기";
        RememberSessionState(AppSessionState.RelationshipMapSurface);
        StatusText.Text = "관계도 화면";
    }

    private void ShowNativeWorkbenchChrome()
    {
        NativeCommandChrome.Visibility = Visibility.Visible;
        StatusText.Visibility = Visibility.Visible;
        MetricsText.Visibility = Visibility.Visible;
    }

    private void HideNativeWorkbenchChrome()
    {
        NativeCommandChrome.Visibility = Visibility.Collapsed;
        StatusText.Visibility = Visibility.Collapsed;
        MetricsText.Visibility = Visibility.Collapsed;
    }

    private bool TryClaimMainSurface(string surfaceId)
    {
        if (_surfaceClaims.TryClaim(WorkbenchSurfaceClaimRegistry.MainOwnerId, surfaceId, out var occupiedBy))
        {
            RefreshDetachedSurfaceAvailability();
            return true;
        }

        var surfaceName = WorkbenchSurfaceCatalog.GetName(surfaceId);
        StatusText.Text = $"{surfaceName} 화면은 이미 다른 분리 작업대에서 사용 중입니다. ({occupiedBy})";
        return false;
    }

    private void RefreshDetachedSurfaceAvailability()
    {
        foreach (var window in _detachedWorkbenchWindows.ToList())
        {
            window.RefreshSurfaceAvailability();
        }
    }

    private async void ReturnToEditorButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenEditorSurfaceAsync();
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

    private bool TryReadFocusDurationMinutes(out int minutes)
    {
        minutes = _focusDurationMinutes;
        var raw = FocusDurationMinutesBox.Text.Trim();
        if (!int.TryParse(raw, out var parsed))
        {
            return false;
        }

        if (parsed is < MinFocusDurationMinutes or > MaxFocusDurationMinutes)
        {
            return false;
        }

        minutes = parsed;
        return true;
    }

    private void ApplyFocusDurationMinutes(int minutes)
    {
        _focusDurationMinutes = Math.Clamp(minutes, MinFocusDurationMinutes, MaxFocusDurationMinutes);
        FocusDurationMinutesBox.Text = _focusDurationMinutes.ToString();
        UpdateFocusButtonIdleContent();
    }

    private void UpdateFocusButtonIdleContent()
    {
        if (_focusMode)
        {
            return;
        }

        FocusButton.Content = $"집중 {_focusDurationMinutes:00}:00";
    }

    private void StartFocus()
    {
        if (!TryReadFocusDurationMinutes(out var focusMinutes))
        {
            StatusText.Text = $"집중 시간은 {MinFocusDurationMinutes}~{MaxFocusDurationMinutes}분 사이 숫자로 입력하세요.";
            return;
        }

        _focusDurationMinutes = focusMinutes;
        UpdateFocusButtonIdleContent();
        RememberSessionState(_sessionState.Surface);

        _htmlActiveView = "editor";
        ShowHtmlWorkbenchSurface();
        _focusMode = true;
        var state = _focusSession.Start(new FocusSessionOptions(TimeSpan.FromMinutes(_focusDurationMinutes), 20, true));
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
        UpdateFocusButtonIdleContent();
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
            StoryRelationshipList.IsKeyboardFocusWithin ||
            RelationshipEntityList.IsKeyboardFocusWithin ||
            RelationshipList.IsKeyboardFocusWithin ||
            RelationshipEntityNameBox.IsKeyboardFocusWithin ||
            RelationshipEntityTypeBox.IsKeyboardFocusWithin ||
            RelationshipEntityRoleBox.IsKeyboardFocusWithin ||
            RelationshipEntitySummaryBox.IsKeyboardFocusWithin ||
            RelationshipEntityColorBox.IsKeyboardFocusWithin ||
            RelationshipEntityTagsBox.IsKeyboardFocusWithin ||
            RelationshipSourceBox.IsKeyboardFocusWithin ||
            RelationshipTargetBox.IsKeyboardFocusWithin ||
            RelationshipLabelBox.IsKeyboardFocusWithin ||
            RelationshipNotesBox.IsKeyboardFocusWithin ||
            RelationshipDirectionalBox.IsKeyboardFocusWithin)
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
            _graphicPreset.Id,
            _focusDurationMinutes);
    }

    private async Task PersistSessionStateAsync()
    {
        try
        {
            var persistedSurface = StartupSurfaceResolver.ToPersistedStartupSurface(_sessionState.Surface);
            var stateToSave = new AppSessionState(
                _projectRoot,
                string.IsNullOrWhiteSpace(_activeDocumentId) ? null : _activeDocumentId,
                persistedSurface,
                _lastAppliedPresetSlot ?? _sessionState.PresetSlot,
                _graphicPreset.Id,
                _focusDurationMinutes);
            await _sessionStateService.SaveAsync(stateToSave, CancellationToken.None);
            _projectAppSettings = _projectAppSettings with
            {
                AutosaveEnabled = _autosaveEnabled,
                LastSurface = stateToSave.Surface,
                LastSceneId = stateToSave.DocumentId
            };
            await _projectSettingsStore.SaveAsync(_projectAppSettings, CancellationToken.None);
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

    private sealed record StoryEntityListItem(StoryEntity Entity)
    {
        public string Id => Entity.Id;
        public string Display => $"{Entity.Name} | {Entity.Type} | {Entity.Role}";
    }

    private sealed record RelationshipListItem(
        string Id,
        StoryRelationship Relationship,
        string SourceName,
        string TargetName)
    {
        public string Display =>
            $"{SourceName} -> {TargetName} | {Relationship.Label}";

        public static RelationshipListItem From(
            StoryRelationship relationship,
            IReadOnlyList<StoryEntity> entities)
        {
            var source = entities.FirstOrDefault(entity =>
                string.Equals(entity.Id, relationship.SourceEntityId, StringComparison.OrdinalIgnoreCase));
            var target = entities.FirstOrDefault(entity =>
                string.Equals(entity.Id, relationship.TargetEntityId, StringComparison.OrdinalIgnoreCase));
            return new RelationshipListItem(
                relationship.Id,
                relationship,
                source?.Name ?? relationship.SourceEntityId,
                target?.Name ?? relationship.TargetEntityId);
        }
    }

    private sealed record SceneEntityLinkListItem(
        SceneEntityLink Link,
        string EntityName)
    {
        public string Display => $"{EntityName} | {Link.Role}";

        public static SceneEntityLinkListItem From(
            SceneEntityLink link,
            IReadOnlyList<StoryEntity> entities)
        {
            var entity = entities.FirstOrDefault(item =>
                string.Equals(item.Id, link.EntityId, StringComparison.OrdinalIgnoreCase));
            return new SceneEntityLinkListItem(link, entity?.Name ?? link.EntityId);
        }
    }

    private sealed record StoryEntityTypeOption(string Value)
    {
        public static IReadOnlyList<StoryEntityTypeOption> All { get; } =
        [
            new StoryEntityTypeOption(StoryEntityType.Character.ToString()),
            new StoryEntityTypeOption(StoryEntityType.Faction.ToString()),
            new StoryEntityTypeOption(StoryEntityType.Place.ToString()),
            new StoryEntityTypeOption(StoryEntityType.Event.ToString()),
            new StoryEntityTypeOption(StoryEntityType.Item.ToString()),
            new StoryEntityTypeOption(StoryEntityType.Concept.ToString())
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
