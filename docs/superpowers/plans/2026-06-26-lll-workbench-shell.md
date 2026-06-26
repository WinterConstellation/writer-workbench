# LLL Workbench Shell Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first implementation slice from `docs/reference-locks/lll-structure-to-workbench.md`: split project settings, add widget-registry storage, and make the HTML workbench render from command/widget placement data.

**Architecture:** Keep WPF as the host for storage, commands, shortcuts, and windows. Add small Core stores under `Core/AppSettings`, then extend `WebWorkbenchPayloadFactory` so HTML receives menu and remote-control placement data without hard-coded button lists.

**Tech Stack:** WPF/.NET, C# records, JSON stores using `System.Text.Json`, WebView2 HTML/CSS/JS.

## Global Constraints

- Do not copy LLL code, assets, icons, names, file formats, WebView cache, IndexedDB, Login Data, or user manuscripts.
- HTML/WebView2 should be the primary workbench surface; WPF stays the host for files, windows, shortcuts, and native commands.
- Export, snapshot, full index rebuild, and full manuscript scan must stay out of the input loop.
- Use TDD for new store and payload behavior.
- Verify with `dotnet build WriterWorkbench.sln --no-restore` and `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore`.

---

### Task 1: Project Settings Paths And Stores

**Files:**
- Modify: `src/WriterWorkbench/Core/Storage/ProjectPaths.cs`
- Create: `src/WriterWorkbench/Core/AppSettings/ProjectAppSettings.cs`
- Create: `src/WriterWorkbench/Core/AppSettings/ProjectSettingsStore.cs`
- Test: `tests/WriterWorkbench.Tests/ProjectSettingsStoreTests.cs`

**Interfaces:**
- Produces `ProjectPaths.AppSettingsPath`, `EditorProfilesPath`, `WorkspaceOptionsPath`, `WidgetRegistryPath`, `CommandAssignmentsPath`, `TextReplacementsPath`, `ExportProfilesPath`, `PathRestorerPath`, `MigrationStatePath`.
- Produces `ProjectSettingsStore.LoadOrCreateAsync(CancellationToken)` and `ProjectSettingsStore.SaveAsync(ProjectAppSettings, CancellationToken)`.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task MissingAppSettingsCreatesDefaultFileWithKoreanSafeJson()
{
    var root = NewRoot();
    var paths = ProjectPaths.ForRoot(root);
    var store = new ProjectSettingsStore(paths.AppSettingsPath);

    var settings = await store.LoadOrCreateAsync(CancellationToken.None);

    Assert.Equal("app", settings.Schema);
    Assert.True(settings.AutosaveEnabled);
    Assert.True(File.Exists(paths.AppSettingsPath));
    Assert.Contains("마지막 작업", await File.ReadAllTextAsync(paths.AppSettingsPath));
}

[Fact]
public void ProjectPathsExposeSeparatedSettingsFiles()
{
    var paths = ProjectPaths.ForRoot(@"C:\WriterWorkbench\Sample.writerproj");

    Assert.EndsWith(@"settings\app.json", paths.AppSettingsPath);
    Assert.EndsWith(@"settings\editor-profiles.json", paths.EditorProfilesPath);
    Assert.EndsWith(@"settings\widget-registry.json", paths.WidgetRegistryPath);
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter ProjectSettingsStoreTests`

- [ ] **Step 3: Implement minimal paths and app settings store**

Add path properties to `ProjectPaths`. Add `ProjectAppSettings` and `ProjectSettingsStore` with UTF-8 JSON, relaxed Korean escaping, temp-file save, corrupt JSON fallback to defaults.

- [ ] **Step 4: Run tests and verify they pass**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter ProjectSettingsStoreTests`

### Task 2: Widget Registry Store

**Files:**
- Create: `src/WriterWorkbench/Core/AppSettings/WorkbenchWidgetRegistry.cs`
- Create: `src/WriterWorkbench/Core/AppSettings/WidgetRegistryStore.cs`
- Test: `tests/WriterWorkbench.Tests/WidgetRegistryStoreTests.cs`

**Interfaces:**
- Produces `WidgetRegistryStore.LoadOrCreateAsync(IReadOnlyList<CommandPlacement> defaults, CancellationToken)`.
- Produces `WidgetRegistryStore.SaveAsync(WorkbenchWidgetRegistry registry, CancellationToken)`.
- Uses existing `CommandPlacement` from `Core/Customization`.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task MissingWidgetRegistryCreatesDefaultInstancesFromPlacements()
{
    var paths = ProjectPaths.ForRoot(NewRoot());
    var store = new WidgetRegistryStore(paths.WidgetRegistryPath);
    var defaults = new[]
    {
        new CommandPlacement("menu", "top.project", "save", "project.save", "저장", 10, new Dictionary<string, string>()),
        new CommandPlacement("remote", "floating", "snapshot", "snapshot.createCurrent", "스냅샷", 20, new Dictionary<string, string>())
    };

    var registry = await store.LoadOrCreateAsync(defaults, CancellationToken.None);

    Assert.Equal(2, registry.Instances.Count);
    Assert.Contains(registry.Instances, item => item.Surface == "menu" && item.Area == "top.project");
    Assert.Contains("스냅샷", await File.ReadAllTextAsync(paths.WidgetRegistryPath));
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter WidgetRegistryStoreTests`

- [ ] **Step 3: Implement registry model and store**

Create records for `WorkbenchWidgetRegistry` and `WidgetInstance`. Convert default `CommandPlacement` into widget instances when the file is missing.

- [ ] **Step 4: Run tests and verify they pass**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter WidgetRegistryStoreTests`

### Task 3: Web Workbench Payload Uses Menu And Remote Placements

**Files:**
- Modify: `src/WriterWorkbench/Core/WebWorkbench/WebWorkbenchPayload.cs`
- Modify: `src/WriterWorkbench/Core/WebWorkbench/WebWorkbenchPayloadFactory.cs`
- Test: `tests/WriterWorkbench.Tests/WebWorkbenchPayloadFactoryTests.cs`

**Interfaces:**
- Adds `IReadOnlyList<WebWorkbenchCommand> MenuCommands`.
- Adds `IReadOnlyList<WebWorkbenchCommand> RemoteCommands`.
- Keeps existing `Commands` for compatibility with old HTML.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void PayloadSeparatesTopMenuAndRemoteCommandsFromProfilePlacements()
{
    var payload = WebWorkbenchPayloadFactory.Create(...);

    Assert.Contains(payload.MenuCommands, command => command.Surface == "menu" && command.Area == "top.project");
    Assert.Contains(payload.RemoteCommands, command => command.Surface == "remote" && command.Area == "floating");
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter WebWorkbenchPayloadFactoryTests`

- [ ] **Step 3: Implement placement grouping**

Map profile placements for `menu/*` and `remote/*`. Keep toolbar commands in `Commands` to avoid breaking existing tests and HTML fallback.

- [ ] **Step 4: Run tests and verify they pass**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter WebWorkbenchPayloadFactoryTests`

### Task 4: HTML Workbench Shell Rebuild

**Files:**
- Modify: `src/WriterWorkbench/WebWorkbench/index.html`
- Modify: `src/WriterWorkbench/WebWorkbench/styles.css`
- Modify: `src/WriterWorkbench/WebWorkbench/app.js`
- Test: `tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs`

**Interfaces:**
- Consumes `payload.menuCommands`, `payload.remoteCommands`, `payload.binder`, `payload.activeScene`, `payload.statusText`.
- Sends `{ type: "command", commandId }` back to WPF unchanged.

- [ ] **Step 1: Write or update a smoke test**

Assert the packaged HTML contains stable shell anchors: `top-menu`, `left-rail`, `center-surface`, `right-inspector`, `floating-remote`, and `bottom-status`.

- [ ] **Step 2: Run test and verify it fails**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter MainWindowContainsHtmlWorkbenchShellAnchors`

- [ ] **Step 3: Rebuild HTML/CSS/JS shell**

Replace the current card-heavy shell with a full workbench layout: two-line top menu, left rail, center surface, right inspector, bottom status, and floating remote.

- [ ] **Step 4: Run test and verify it passes**

Run: `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter MainWindowContainsHtmlWorkbenchShellAnchors`

### Task 5: MainWindow Wiring And Verification

**Files:**
- Modify: `src/WriterWorkbench/MainWindow.xaml.cs`
- Modify: `docs/pingpong.txt`

**Interfaces:**
- Initialize project app settings and widget registry when project configuration loads.
- Keep `EditorBox_TextChanged` free of export/snapshot/index/payload rebuild work.

- [ ] **Step 1: Add failing test for generated settings files**

Use an existing MainWindow smoke pattern or focused store test to verify default files are created under project `settings`.

- [ ] **Step 2: Wire stores during project startup**

Load/create app settings and widget registry after project paths are known. Do not block typing path.

- [ ] **Step 3: Run full verification**

Run:

```powershell
dotnet build WriterWorkbench.sln --no-restore
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore
```

- [ ] **Step 4: Publish and sync public repo**

Publish local exe, update `docs/pingpong.txt`, copy changed files to public repo, build/test public repo, commit and push.
