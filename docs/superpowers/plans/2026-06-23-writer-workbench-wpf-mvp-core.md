# Writer Workbench WPF MVP Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first native Windows writer workbench: WPF app, local project folder, active document editor, live preview, command registry, custom shortcuts, autosave/TXT export, and focus session timer.

**Architecture:** WPF owns the desktop UI, windowing, shortcuts, and focus mode. Plain .NET services own command registration, project files, autosave, TXT export, SQLite metadata, and workspace presets. The manuscript source is structured paragraph JSON, not HTML.

**Tech Stack:** WPF, .NET 8, C#, System.Text.Json, Microsoft.Data.Sqlite, xUnit, Windows APIs through .NET where needed.

## Global Constraints

- Windows only.
- No Tauri, Electron, WinUI 3, Qt, Rust, React, Vite, or WebView UI shell.
- Offline-first app behavior.
- No AI/API calls in MVP.
- No embedded login browser in MVP.
- No system clock changes.
- No hard OS lockout.
- Project files default to `C:\WriterWorkbench\Projects`.
- Canonical manuscript source is `*.wwdoc.json`.
- Automatic TXT export is derived from the canonical document.
- SQLite is an index/settings store, not the only recovery source.

---

## Current Machine Boundary

The machine has .NET runtimes but no .NET SDK.

Before implementation, the user must approve exactly one build-tool option:

- Install the official .NET 8 SDK, then build locally.
- Use another existing machine/environment that already has the .NET SDK.
- Write source files only and defer build/run verification.

Do not install anything as part of this plan without explicit approval.

---

## File Structure

Create these files after the build-tool boundary is approved.

```text
src/
  WriterWorkbench/
    WriterWorkbench.csproj
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    Core/
      Commands/
        AppCommand.cs
        CommandRegistry.cs
        CommandScope.cs
        ShortcutBinding.cs
        ShortcutManager.cs
      Documents/
        WriterDocument.cs
        WriterParagraph.cs
        TextExportService.cs
      Storage/
        ProjectManifest.cs
        ProjectPaths.cs
        ProjectStore.cs
        SqliteProjectIndex.cs
      Workspace/
        MonitorRegion.cs
        WorkspacePreset.cs
        WorkspacePresetService.cs
      Focus/
        FocusSessionOptions.cs
        FocusSessionState.cs
        FocusSessionService.cs
    ViewModels/
      ViewModelBase.cs
      MainWindowViewModel.cs
      BinderItemViewModel.cs
      CommandSlotViewModel.cs
    Views/
      BinderView.xaml
      BinderView.xaml.cs
      EditorView.xaml
      EditorView.xaml.cs
      PreviewView.xaml
      PreviewView.xaml.cs
      InspectorView.xaml
      InspectorView.xaml.cs
      FocusOverlayView.xaml
      FocusOverlayView.xaml.cs
tests/
  WriterWorkbench.Tests/
    WriterWorkbench.Tests.csproj
    CommandRegistryTests.cs
    ShortcutManagerTests.cs
    DocumentSerializationTests.cs
    TextExportServiceTests.cs
    WorkspacePresetTests.cs
    FocusSessionServiceTests.cs
```

Responsibility map:

- `Core/Commands/*`: command IDs, scopes, shortcut binding, conflict detection.
- `Core/Documents/*`: paragraph document model and TXT generation.
- `Core/Storage/*`: project paths, manifest, file read/write, SQLite index.
- `Core/Workspace/*`: saved layouts and monitor-region data.
- `Core/Focus/*`: countdown state and early-exit challenge rules.
- `ViewModels/*`: UI state, command slots, save state, active document.
- `Views/*`: WPF surfaces for binder, editor, preview, inspector, focus overlay.

---

## Task 1: Lock Build Boundary

**Files:**
- No source files.

**Interfaces:**
- Produces the approved build-tool path for later tasks.

- [ ] **Step 1: Confirm SDK state**

Run:

```powershell
dotnet --info
```

Expected before approval: `.NET SDKs installed: No SDKs were found.`

- [ ] **Step 2: Ask user for build-tool approval**

Use this exact boundary:

```text
현재는 .NET 런타임만 있고 SDK가 없습니다. WPF 앱을 빌드하려면 공식 .NET 8 SDK가 필요합니다.
설치 승인 전에는 코드 파일만 만들 수 있고 실행 검증은 못 합니다.
```

- [ ] **Step 3: Continue only after approval**

Expected:

```text
User explicitly approves SDK install, or chooses source-only work.
```

---

## Task 2: Create Solution Skeleton

**Files:**
- Create: `src/WriterWorkbench/WriterWorkbench.csproj`
- Create: `src/WriterWorkbench/App.xaml`
- Create: `src/WriterWorkbench/App.xaml.cs`
- Create: `src/WriterWorkbench/MainWindow.xaml`
- Create: `src/WriterWorkbench/MainWindow.xaml.cs`
- Create: `tests/WriterWorkbench.Tests/WriterWorkbench.Tests.csproj`

**Interfaces:**
- Produces a WPF app project named `WriterWorkbench`.
- Produces a test project referencing the app project.

- [ ] **Step 1: Create directories**

```powershell
New-Item -ItemType Directory -Force src\WriterWorkbench, tests\WriterWorkbench.Tests
```

- [ ] **Step 2: Create project files**

`src/WriterWorkbench/WriterWorkbench.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
  </ItemGroup>
</Project>
```

`tests/WriterWorkbench.Tests/WriterWorkbench.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <ProjectReference Include="..\..\src\WriterWorkbench\WriterWorkbench.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create minimal WPF app**

`src/WriterWorkbench/App.xaml`:

```xml
<Application x:Class="WriterWorkbench.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
</Application>
```

`src/WriterWorkbench/App.xaml.cs`:

```csharp
using System.Windows;

namespace WriterWorkbench;

public partial class App : Application
{
}
```

`src/WriterWorkbench/MainWindow.xaml`:

```xml
<Window x:Class="WriterWorkbench.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Writer Workbench"
        Width="1280"
        Height="800"
        MinWidth="960"
        MinHeight="640">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="260"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="320"/>
        </Grid.ColumnDefinitions>

        <Border Grid.Column="0" Background="#F3F4F6" BorderBrush="#D1D5DB" BorderThickness="0,0,1,0"/>
        <Border Grid.Column="1" Background="#FFFFFF"/>
        <Border Grid.Column="2" Background="#FAFAFA" BorderBrush="#D1D5DB" BorderThickness="1,0,0,0"/>
    </Grid>
</Window>
```

`src/WriterWorkbench/MainWindow.xaml.cs`:

```csharp
using System.Windows;

namespace WriterWorkbench;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 4: Verify build**

Run:

```powershell
dotnet build src\WriterWorkbench\WriterWorkbench.csproj
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj
```

Expected: build succeeds; tests project runs with zero tests or test discovery succeeds.

---

## Task 3: Command Registry And Shortcut Manager

**Files:**
- Create: `src/WriterWorkbench/Core/Commands/AppCommand.cs`
- Create: `src/WriterWorkbench/Core/Commands/CommandRegistry.cs`
- Create: `src/WriterWorkbench/Core/Commands/CommandScope.cs`
- Create: `src/WriterWorkbench/Core/Commands/ShortcutBinding.cs`
- Create: `src/WriterWorkbench/Core/Commands/ShortcutManager.cs`
- Create: `tests/WriterWorkbench.Tests/CommandRegistryTests.cs`
- Create: `tests/WriterWorkbench.Tests/ShortcutManagerTests.cs`

**Interfaces:**
- Produces `CommandRegistry.Register(AppCommand command)`.
- Produces `CommandRegistry.Get(string id)`.
- Produces `ShortcutManager.TryBind(ShortcutBinding binding, out string? conflictCommandId)`.

- [ ] **Step 1: Add command tests**

`tests/WriterWorkbench.Tests/CommandRegistryTests.cs`:

```csharp
using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class CommandRegistryTests
{
    [Fact]
    public void RegistersAndReturnsCommandById()
    {
        var registry = new CommandRegistry();
        var command = new AppCommand("project.save", "Save", "Project", CommandScope.Editor);

        registry.Register(command);

        Assert.Equal(command, registry.Get("project.save"));
    }
}
```

`tests/WriterWorkbench.Tests/ShortcutManagerTests.cs`:

```csharp
using WriterWorkbench.Core.Commands;

namespace WriterWorkbench.Tests;

public sealed class ShortcutManagerTests
{
    [Fact]
    public void DetectsConflictInsideSameScope()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Editor), out _));
        Assert.False(manager.TryBind(new ShortcutBinding("document.split", "Ctrl+S", CommandScope.Editor), out var conflict));
        Assert.Equal("project.save", conflict);
    }

    [Fact]
    public void AllowsSameShortcutAcrossDifferentScopes()
    {
        var manager = new ShortcutManager();

        Assert.True(manager.TryBind(new ShortcutBinding("project.save", "Ctrl+S", CommandScope.Editor), out _));
        Assert.True(manager.TryBind(new ShortcutBinding("binder.rename", "Ctrl+S", CommandScope.Binder), out _));
    }
}
```

- [ ] **Step 2: Implement command types**

`src/WriterWorkbench/Core/Commands/CommandScope.cs`:

```csharp
namespace WriterWorkbench.Core.Commands;

public enum CommandScope
{
    Global,
    Workbench,
    Editor,
    Binder,
    Preview,
    FocusSession
}
```

`src/WriterWorkbench/Core/Commands/AppCommand.cs`:

```csharp
namespace WriterWorkbench.Core.Commands;

public sealed record AppCommand(
    string Id,
    string Name,
    string Category,
    CommandScope DefaultScope
);
```

`src/WriterWorkbench/Core/Commands/ShortcutBinding.cs`:

```csharp
namespace WriterWorkbench.Core.Commands;

public sealed record ShortcutBinding(
    string CommandId,
    string Gesture,
    CommandScope Scope
);
```

`src/WriterWorkbench/Core/Commands/CommandRegistry.cs`:

```csharp
namespace WriterWorkbench.Core.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, AppCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(AppCommand command)
    {
        if (!_commands.TryAdd(command.Id, command))
        {
            throw new InvalidOperationException($"Command already registered: {command.Id}");
        }
    }

    public AppCommand Get(string id)
    {
        return _commands.TryGetValue(id, out var command)
            ? command
            : throw new KeyNotFoundException($"Command not found: {id}");
    }
}
```

`src/WriterWorkbench/Core/Commands/ShortcutManager.cs`:

```csharp
namespace WriterWorkbench.Core.Commands;

public sealed class ShortcutManager
{
    private readonly List<ShortcutBinding> _bindings = [];

    public bool TryBind(ShortcutBinding binding, out string? conflictCommandId)
    {
        var conflict = _bindings.FirstOrDefault(existing =>
            existing.Scope == binding.Scope &&
            string.Equals(existing.Gesture, binding.Gesture, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.CommandId, binding.CommandId, StringComparison.OrdinalIgnoreCase));

        if (conflict is not null)
        {
            conflictCommandId = conflict.CommandId;
            return false;
        }

        _bindings.RemoveAll(existing =>
            existing.Scope == binding.Scope &&
            string.Equals(existing.CommandId, binding.CommandId, StringComparison.OrdinalIgnoreCase));
        _bindings.Add(binding);
        conflictCommandId = null;
        return true;
    }
}
```

- [ ] **Step 3: Verify**

Run:

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --filter "CommandRegistryTests|ShortcutManagerTests"
```

Expected: all command and shortcut tests pass.

---

## Task 4: Document Model And TXT Export

**Files:**
- Create: `src/WriterWorkbench/Core/Documents/WriterParagraph.cs`
- Create: `src/WriterWorkbench/Core/Documents/WriterDocument.cs`
- Create: `src/WriterWorkbench/Core/Documents/TextExportService.cs`
- Create: `tests/WriterWorkbench.Tests/DocumentSerializationTests.cs`
- Create: `tests/WriterWorkbench.Tests/TextExportServiceTests.cs`

**Interfaces:**
- Produces `WriterDocument`.
- Produces `TextExportService.ToPlainText(WriterDocument document)`.

- [ ] **Step 1: Add tests**

`tests/WriterWorkbench.Tests/TextExportServiceTests.cs`:

```csharp
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class TextExportServiceTests
{
    [Fact]
    public void ExportsParagraphsWithBlankLineBetweenThem()
    {
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [
                new WriterParagraph("p-1", "첫 문단입니다.", "body", [], []),
                new WriterParagraph("p-2", "Second paragraph.", "body", [], [])
            ]);

        var text = TextExportService.ToPlainText(document);

        Assert.Equal("첫 문단입니다.\r\n\r\nSecond paragraph.", text);
    }
}
```

`tests/WriterWorkbench.Tests/DocumentSerializationTests.cs`:

```csharp
using System.Text.Json;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Tests;

public sealed class DocumentSerializationTests
{
    [Fact]
    public void SerializesAndDeserializesDocument()
    {
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [new WriterParagraph("p-1", "본문", "body", ["draft"], ["memo"])]);

        var json = JsonSerializer.Serialize(document);
        var loaded = JsonSerializer.Deserialize<WriterDocument>(json);

        Assert.NotNull(loaded);
        Assert.Equal("scene-0001", loaded.Id);
        Assert.Equal("본문", loaded.Paragraphs[0].Text);
    }
}
```

- [ ] **Step 2: Implement document model**

`src/WriterWorkbench/Core/Documents/WriterParagraph.cs`:

```csharp
namespace WriterWorkbench.Core.Documents;

public sealed record WriterParagraph(
    string Id,
    string Text,
    string Style,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Notes
);
```

`src/WriterWorkbench/Core/Documents/WriterDocument.cs`:

```csharp
namespace WriterWorkbench.Core.Documents;

public sealed record WriterDocument(
    string Id,
    string Title,
    IReadOnlyList<WriterParagraph> Paragraphs
);
```

`src/WriterWorkbench/Core/Documents/TextExportService.cs`:

```csharp
namespace WriterWorkbench.Core.Documents;

public static class TextExportService
{
    public static string ToPlainText(WriterDocument document)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            document.Paragraphs.Select(paragraph => paragraph.Text.TrimEnd()));
    }
}
```

- [ ] **Step 3: Verify**

Run:

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --filter "DocumentSerializationTests|TextExportServiceTests"
```

Expected: all document tests pass.

---

## Task 5: Project Storage And Autosave Surface

**Files:**
- Create: `src/WriterWorkbench/Core/Storage/ProjectPaths.cs`
- Create: `src/WriterWorkbench/Core/Storage/ProjectManifest.cs`
- Create: `src/WriterWorkbench/Core/Storage/ProjectStore.cs`
- Create: `src/WriterWorkbench/Core/Storage/SqliteProjectIndex.cs`
- Create: `tests/WriterWorkbench.Tests/ProjectStoreTests.cs`

**Interfaces:**
- Produces `ProjectPaths.ForRoot(string rootPath)`.
- Produces `ProjectStore.SaveDocumentAsync(WriterDocument document, CancellationToken token)`.
- Produces canonical `*.wwdoc.json` and derived `*.txt`.

Implementation rules:

- Write to `.tmp` first.
- Replace target only after the temporary file is complete.
- Save canonical document and TXT export together.
- Update SQLite index after file save succeeds.

- [ ] **Step 1: Add storage test**

`tests/WriterWorkbench.Tests/ProjectStoreTests.cs`:

```csharp
using WriterWorkbench.Core.Documents;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Tests;

public sealed class ProjectStoreTests
{
    [Fact]
    public async Task SaveDocumentWritesCanonicalJsonAndDerivedTxt()
    {
        var root = Path.Combine(Path.GetTempPath(), "WriterWorkbenchTests", Guid.NewGuid().ToString("N"));
        var paths = ProjectPaths.ForRoot(root);
        var store = new ProjectStore(paths);
        var document = new WriterDocument(
            "scene-0001",
            "Opening",
            [new WriterParagraph("p-1", "첫 문단", "body", [], [])]);

        await store.SaveDocumentAsync(document, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(root, "documents", "scene-0001.wwdoc.json")));
        Assert.Equal("첫 문단", await File.ReadAllTextAsync(Path.Combine(root, "documents", "scene-0001.txt")));
    }
}
```

- [ ] **Step 2: Implement storage paths**

`src/WriterWorkbench/Core/Storage/ProjectPaths.cs`:

```csharp
namespace WriterWorkbench.Core.Storage;

public sealed record ProjectPaths(string RootPath)
{
    public string DocumentsPath => Path.Combine(RootPath, "documents");
    public string ProjectDatabasePath => Path.Combine(RootPath, "project.sqlite");
    public string ManifestPath => Path.Combine(RootPath, "project.manifest.json");

    public static ProjectPaths ForRoot(string rootPath)
    {
        return new ProjectPaths(rootPath);
    }

    public string DocumentJsonPath(string documentId)
    {
        return Path.Combine(DocumentsPath, $"{documentId}.wwdoc.json");
    }

    public string DocumentTextPath(string documentId)
    {
        return Path.Combine(DocumentsPath, $"{documentId}.txt");
    }
}
```

- [ ] **Step 3: Implement manifest shell**

`src/WriterWorkbench/Core/Storage/ProjectManifest.cs`:

```csharp
namespace WriterWorkbench.Core.Storage;

public sealed record ProjectManifest(
    int SchemaVersion,
    string Title,
    IReadOnlyList<string> DocumentIds
);
```

- [ ] **Step 4: Implement atomic project save**

`src/WriterWorkbench/Core/Storage/ProjectStore.cs`:

```csharp
using System.Text.Json;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Core.Storage;

public sealed class ProjectStore(ProjectPaths paths)
{
    public async Task SaveDocumentAsync(WriterDocument document, CancellationToken token)
    {
        Directory.CreateDirectory(paths.DocumentsPath);

        var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        await WriteUtf8AtomicAsync(paths.DocumentJsonPath(document.Id), json, token);
        await WriteUtf8AtomicAsync(paths.DocumentTextPath(document.Id), TextExportService.ToPlainText(document), token);
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, string content, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, content, token);
        File.Move(tempPath, targetPath, overwrite: true);
    }
}
```

- [ ] **Step 5: Add SQLite index shell**

`src/WriterWorkbench/Core/Storage/SqliteProjectIndex.cs`:

```csharp
namespace WriterWorkbench.Core.Storage;

public sealed class SqliteProjectIndex
{
    public string DatabasePath { get; }

    public SqliteProjectIndex(string databasePath)
    {
        DatabasePath = databasePath;
    }
}
```

- [ ] **Step 6: Verify**

Run:

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --filter ProjectStoreTests
```

Expected: storage test passes and temp project contains `documents\scene-0001.wwdoc.json` plus `documents\scene-0001.txt`.

---

## Task 6: Workbench UI

**Files:**
- Create: `src/WriterWorkbench/ViewModels/ViewModelBase.cs`
- Create: `src/WriterWorkbench/ViewModels/MainWindowViewModel.cs`
- Create: `src/WriterWorkbench/ViewModels/BinderItemViewModel.cs`
- Create: `src/WriterWorkbench/ViewModels/CommandSlotViewModel.cs`
- Create: `src/WriterWorkbench/Views/BinderView.xaml`
- Create: `src/WriterWorkbench/Views/EditorView.xaml`
- Create: `src/WriterWorkbench/Views/PreviewView.xaml`
- Create: `src/WriterWorkbench/Views/InspectorView.xaml`
- Modify: `src/WriterWorkbench/MainWindow.xaml`

**Interfaces:**
- Consumes command registry and active document model.
- Produces first workbench screen with binder, editor, preview, inspector, command bar, and status strip.

MVP UI rules:

- No landing page.
- Main screen is the workbench.
- Text must not overflow buttons or panels.
- Avoid decorative cards.
- Keep panes dense and utilitarian.

---

## Task 7: Focus Session

**Files:**
- Create: `src/WriterWorkbench/Core/Focus/FocusSessionOptions.cs`
- Create: `src/WriterWorkbench/Core/Focus/FocusSessionState.cs`
- Create: `src/WriterWorkbench/Core/Focus/FocusSessionService.cs`
- Create: `src/WriterWorkbench/Views/FocusOverlayView.xaml`
- Create: `tests/WriterWorkbench.Tests/FocusSessionServiceTests.cs`

**Interfaces:**
- Produces `FocusSessionService.Start(FocusSessionOptions options)`.
- Produces `FocusSessionService.CanExitEarly(string confirmationText)`.

Required behavior:

- MVP default timer is 40 minutes.
- Default early-exit confirmation minimum is 20 characters.
- Fullscreen is app-level only.
- `Ctrl+S` stays available.
- No OS-level lockout.

- [ ] **Step 1: Add focus tests**

`tests/WriterWorkbench.Tests/FocusSessionServiceTests.cs`:

```csharp
using WriterWorkbench.Core.Focus;

namespace WriterWorkbench.Tests;

public sealed class FocusSessionServiceTests
{
    [Fact]
    public void RejectsShortEarlyExitConfirmation()
    {
        var service = new FocusSessionService();
        service.Start(new FocusSessionOptions(TimeSpan.FromMinutes(40), 20, true));

        Assert.False(service.CanExitEarly("too short"));
        Assert.True(service.CanExitEarly("12345678901234567890"));
    }
}
```

- [ ] **Step 2: Implement focus service**

`src/WriterWorkbench/Core/Focus/FocusSessionOptions.cs`:

```csharp
namespace WriterWorkbench.Core.Focus;

public sealed record FocusSessionOptions(
    TimeSpan Duration,
    int ExitConfirmMinChars,
    bool EnterFullscreen
);
```

`src/WriterWorkbench/Core/Focus/FocusSessionState.cs`:

```csharp
namespace WriterWorkbench.Core.Focus;

public sealed record FocusSessionState(
    DateTimeOffset StartedAt,
    DateTimeOffset EndsAt,
    FocusSessionOptions Options
);
```

`src/WriterWorkbench/Core/Focus/FocusSessionService.cs`:

```csharp
namespace WriterWorkbench.Core.Focus;

public sealed class FocusSessionService
{
    private FocusSessionState? _state;

    public FocusSessionState Start(FocusSessionOptions options)
    {
        var now = DateTimeOffset.Now;
        _state = new FocusSessionState(now, now.Add(options.Duration), options);
        return _state;
    }

    public bool CanExitEarly(string confirmationText)
    {
        return _state is not null &&
               confirmationText.Trim().Length >= _state.Options.ExitConfirmMinChars;
    }
}
```

- [ ] **Step 3: Verify**

Run:

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --filter FocusSessionServiceTests
```

Expected: focus confirmation test passes.

---

## Task 8: Workspace Presets

**Files:**
- Create: `src/WriterWorkbench/Core/Workspace/MonitorRegion.cs`
- Create: `src/WriterWorkbench/Core/Workspace/WorkspacePreset.cs`
- Create: `src/WriterWorkbench/Core/Workspace/WorkspacePresetService.cs`
- Create: `tests/WriterWorkbench.Tests/WorkspacePresetTests.cs`

**Interfaces:**
- Produces `WorkspacePresetService.SaveCurrent(...)`.
- Produces `WorkspacePresetService.Apply(...)`.

MVP behavior:

- Save and apply one-monitor layout.
- Store preset 1, 2, 3.
- Store startup auto-apply on/off.
- Multi-monitor support is excluded from MVP 0.1 and starts in MVP 0.2.

- [ ] **Step 1: Add workspace preset test**

`tests/WriterWorkbench.Tests/WorkspacePresetTests.cs`:

```csharp
using WriterWorkbench.Core.Workspace;

namespace WriterWorkbench.Tests;

public sealed class WorkspacePresetTests
{
    [Fact]
    public void StoresPresetSlotAndStartupFlag()
    {
        var preset = new WorkspacePreset(
            Slot: 1,
            Name: "Main desk",
            Region: MonitorRegion.Full,
            AutoApplyOnStartup: true);

        Assert.Equal(1, preset.Slot);
        Assert.True(preset.AutoApplyOnStartup);
        Assert.Equal(MonitorRegion.Full, preset.Region);
    }
}
```

- [ ] **Step 2: Implement workspace preset model**

`src/WriterWorkbench/Core/Workspace/MonitorRegion.cs`:

```csharp
namespace WriterWorkbench.Core.Workspace;

public enum MonitorRegion
{
    Full,
    LeftHalf,
    RightHalf,
    TopHalf,
    BottomHalf
}
```

`src/WriterWorkbench/Core/Workspace/WorkspacePreset.cs`:

```csharp
namespace WriterWorkbench.Core.Workspace;

public sealed record WorkspacePreset(
    int Slot,
    string Name,
    MonitorRegion Region,
    bool AutoApplyOnStartup
);
```

`src/WriterWorkbench/Core/Workspace/WorkspacePresetService.cs`:

```csharp
namespace WriterWorkbench.Core.Workspace;

public sealed class WorkspacePresetService
{
    private readonly Dictionary<int, WorkspacePreset> _presets = [];

    public void Save(WorkspacePreset preset)
    {
        if (preset.Slot is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(preset), "MVP supports preset slots 1, 2, and 3.");
        }

        _presets[preset.Slot] = preset;
    }

    public WorkspacePreset? Get(int slot)
    {
        return _presets.GetValueOrDefault(slot);
    }
}
```

- [ ] **Step 3: Verify**

Run:

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --filter WorkspacePresetTests
```

Expected: workspace preset test passes.

---

## Verification Before First Run Claim

Do not claim the app runs until all of these are true:

```powershell
dotnet build src\WriterWorkbench\WriterWorkbench.csproj
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj
dotnet run --project src\WriterWorkbench\WriterWorkbench.csproj
```

Expected:

- Build succeeds.
- Tests pass.
- WPF window opens to workbench mode.
- Manual save writes `*.wwdoc.json` and `*.txt`.
- Preview updates from the active document.

---

## Implementation Order

1. Build boundary approval.
2. WPF skeleton.
3. Command registry and shortcuts.
4. Document model and TXT export.
5. Project storage and SQLite index.
6. Workbench UI.
7. Focus session.
8. Workspace presets.

This order keeps the custom cockpit core ahead of cosmetic UI expansion.
