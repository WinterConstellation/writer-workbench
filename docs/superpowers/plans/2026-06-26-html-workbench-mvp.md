# HTML Workbench MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first WebView2 local HTML workbench surface so Writer Workbench can move design-heavy screens away from raw WPF controls without replacing the Windows-native engine.

**Architecture:** WPF remains the executable host, storage engine, command dispatcher, shortcut layer, window manager, and fallback UI. A local HTML/CSS/JS surface renders a better-looking workbench dashboard and sends command IDs back to WPF through a narrow bridge. The bridge never exports every keystroke; text editing remains in the existing editor for this MVP.

**Tech Stack:** .NET 8 WPF, Microsoft WebView2, local HTML/CSS/JS assets, System.Text.Json.

## Global Constraints

- Windows-only desktop app remains the target.
- Offline by default; HTML assets are local files bundled with the app.
- Do not replace storage, autosave, snapshot, export, binder, or command engine in this task.
- Do not send large manuscript body text through the WebView2 bridge in this MVP.
- Existing WPF editor and relationship map remain as fallback surfaces.
- Update `docs/pingpong.txt`, publish local executable, and push public repo when complete.

---

### Task 1: Web Workbench Payload Foundation

**Files:**
- Create: `src/WriterWorkbench/Core/WebWorkbench/WebWorkbenchPayload.cs`
- Create: `src/WriterWorkbench/Core/WebWorkbench/WebWorkbenchPayloadFactory.cs`
- Test: `tests/WriterWorkbench.Tests/WebWorkbenchPayloadFactoryTests.cs`

**Interfaces:**
- Produces: `WebWorkbenchPayloadFactory.Create(...)`
- Produces JSON-safe records for project title/path, active scene summary, binder items, toolbar commands, and status text.

- [ ] Write tests proving the payload contains Korean title/status text, active scene metadata, binder ordering, and command placements without manuscript body text.
- [ ] Implement the records and factory.
- [ ] Verify targeted tests pass.

### Task 2: Local HTML Assets

**Files:**
- Create: `src/WriterWorkbench/WebWorkbench/index.html`
- Create: `src/WriterWorkbench/WebWorkbench/styles.css`
- Create: `src/WriterWorkbench/WebWorkbench/app.js`
- Modify: `src/WriterWorkbench/WriterWorkbench.csproj`
- Test: `tests/WriterWorkbench.Tests/WebWorkbenchAssetTests.cs`

**Interfaces:**
- Produces bundled local content under `WebWorkbench/`.
- `app.js` consumes messages shaped as `{ type: "state", payload: WebWorkbenchPayload }`.
- `app.js` sends `{ type: "command", commandId: string }` to WPF.

- [ ] Write tests proving required asset files exist and are copied as content.
- [ ] Add HTML/CSS/JS workbench shell with a top command strip, scene list, active scene panel, pipeline panel, and status bar.
- [ ] Verify targeted tests pass.

### Task 3: WebView2 Host And Bridge

**Files:**
- Modify: `src/WriterWorkbench/WriterWorkbench.csproj`
- Modify: `src/WriterWorkbench/MainWindow.xaml`
- Modify: `src/WriterWorkbench/MainWindow.xaml.cs`
- Test: `tests/WriterWorkbench.Tests/AppCommandCatalogTests.cs`
- Test: `tests/WriterWorkbench.Tests/MainWindowSmokeTests.cs`

**Interfaces:**
- Adds command `view.htmlWorkbench.open`.
- Adds `HtmlWorkbenchSurface` as a main workbench surface.
- WPF sends state to WebView2 after project/document/binder/profile/status changes.
- WebView2 command messages call existing `ExecuteCommandAsync`.

- [ ] Write tests proving the new command is registered and MainWindow still constructs on STA.
- [ ] Add WebView2 dependency and XAML host.
- [ ] Add initialization, local asset navigation, JSON state push, and command message handling.
- [ ] Verify targeted tests pass.

### Task 4: Verification, Publish, Handoff, Push

**Files:**
- Modify: `docs/pingpong.txt`

- [ ] Run build and full test suite locally.
- [ ] Publish `dist/WriterWorkbench-menu-remote/WriterWorkbench.exe`.
- [ ] Copy changed files to the public repo.
- [ ] Run build and full test suite in the public repo.
- [ ] Commit and push to `WinterConstellation/writer-workbench`.
