# Windows Writer Workbench WPF Design

## Status

This design supersedes `docs/superpowers/specs/2026-06-22-writer-workbench-design.md`.

The old document assumed Tauri, React, Rust, WebView2, and HTML as the canonical manuscript format. That assumption is no longer valid for this project.

## Goal

Build a Windows-only native writing workbench for long-form manuscript work.

The app is not a Scrivener clone. Binder, outline, notes, autosave, search, and export are expected baseline furniture. The differentiator is a customizable author cockpit: every important action is represented as a command that can be assigned to shortcuts, toolbar slots, panel buttons, context menus, workspace presets, and later macro sequences.

Primary requirements:

- Windows executable.
- Lightweight runtime footprint.
- Fast with large manuscripts.
- Offline-first.
- Local project folders on the C drive by default.
- Automatic TXT export for recovery and external use.
- Extensible later for APIs, local algorithms, visualization, proofreading, and custom analysis.
- Easy tab/window separation for multi-monitor work.
- Workspace presets that can restore multi-window layouts by shortcut or on startup.

## Platform Decision

Use WPF / .NET as the default implementation stack.

Reasons:

- The target platform is Windows only.
- Native window control, global/local shortcuts, monitor placement, fullscreen modes, and file dialogs matter more than cross-platform packaging.
- WPF has mature command binding, routed input, data binding, layout, and desktop integration.
- .NET gives direct access to local files, SQLite, background workers, Windows APIs, and later API clients without a browser shell.
- This avoids Tauri/WebView2 as a hidden UI premise and avoids Qt licensing/tooling overhead for the first implementation.

Do not use Tauri, Electron, WinUI 3, or Qt unless the user explicitly reopens that decision.

## Current Tooling State

The machine currently has .NET runtimes installed but no .NET SDK.

That means:

- Existing .NET apps can run.
- New WPF code cannot be built on this machine until a .NET SDK or equivalent build tool is installed.
- No installation should happen as part of this design.
- Implementation can begin only after the user approves the minimal build tool boundary.

## Product Shape

The first screen is the workbench mode.

Workbench mode contains:

- Binder: document tree, scenes, notes, references.
- Editor: one active document at a time.
- Preview: live manuscript preview from the current document.
- Inspector: synopsis, notes, tags, status, target counts.
- Command bar: user-assignable command slots.
- Status strip: save state, session timer, character count, word count, active preset.

The app has three conceptual modes:

- Workbench mode: normal writing and organization.
- Focus session mode: fullscreen writing with countdown and exit confirmation.
- Review/export mode: preview, search, outline, cleanup, and export workflows.

Mode switching is command-driven, not hardwired to fixed buttons only.

## Command Core

Every user-facing action is registered as a command.

Command record:

```json
{
  "id": "writing.focus.start",
  "name": "Start focus session",
  "category": "Writing",
  "defaultShortcut": "Ctrl+Alt+Enter",
  "parameters": {
    "durationMinutes": 40,
    "exitConfirmMinChars": 20,
    "enterFullscreen": true
  },
  "assignableTo": ["shortcut", "toolbar", "panel", "contextMenu", "workspacePreset"]
}
```

MVP command groups:

- Project commands: create, open, close, save, export TXT.
- Document commands: create scene, rename, move, include/exclude from manuscript.
- Editor commands: save, undo, redo, basic formatting, find.
- Workspace commands: save preset, apply preset, toggle startup preset.
- Focus commands: start timer, pause timer, end session, show exit challenge.
- UI commands: show/hide panels, split tab to window, dock window to monitor region.

The command registry is a first-class module from day one. Shortcuts and UI slots point to command IDs rather than directly calling random UI methods.

## Shortcut Model

Shortcuts are customizable and scoped.

Scopes:

- Global app scope.
- Workbench scope.
- Editor scope.
- Binder scope.
- Preview scope.
- Focus session scope.

Default shortcuts:

- `Ctrl+S`: manual save.
- `Ctrl+1`, `Ctrl+2`, `Ctrl+3`: switch work mode.
- `Ctrl+Alt+1`, `Ctrl+Alt+2`, `Ctrl+Alt+3`: apply workspace preset 1, 2, 3.
- `Ctrl+Alt+Enter`: start focus session.

Shortcut settings must detect conflicts within the same scope. The same key can exist in different scopes only when the active UI context makes the result unambiguous.

## Workspace Presets

The default layout assumes one monitor.

Workspace presets can store:

- Main window bounds.
- Detached tab windows.
- Monitor assignment chosen by the user.
- Region assignment: full, left half, right half, top half, bottom half.
- Open panels.
- Active mode.
- Optional startup auto-apply setting.

Preset behavior:

- A preset can be applied by shortcut.
- A preset can be automatically applied on startup.
- Preset 1, 2, 3 each have independent startup on/off flags.
- If monitors changed, the app does not guess aggressively. It shows the saved monitor labels and current monitor list, then falls back to primary monitor if a target is unavailable.

This is a user-controlled layout restore system, not a fragile automatic monitor identity matcher.

## Focus Session Mode

Focus session mode is a soft writing lock, not OS-level control.

Behavior:

- Starts from a command.
- Enters fullscreen if configured.
- Shows countdown inside the app.
- Keeps the writing surface dominant until the timer ends.
- To exit early, the user types a configurable confirmation phrase or a minimum-length confirmation text.
- Default early-exit requirement is 20 characters.
- `Ctrl+S` remains available.
- Autosave remains active.

Do not change the Windows system clock.
Do not block Task Manager.
Do not intercept OS security shortcuts.
Do not behave like malware.

## Storage Model

Default project root:

```text
C:\WriterWorkbench\Projects\
```

Project folder:

```text
MyNovel.writerproj\
  project.sqlite
  project.manifest.json
  documents\
    scene-0001.wwdoc.json
    scene-0001.txt
    scene-0001.meta.json
    scene-0002.wwdoc.json
    scene-0002.txt
    scene-0002.meta.json
  exports\
  backups\
  assets\
```

Canonical document format is not HTML.

The canonical manuscript document is a structured paragraph document:

```json
{
  "schemaVersion": 1,
  "id": "scene-0001",
  "title": "Opening",
  "paragraphs": [
    {
      "id": "p-0001",
      "text": "Paragraph text",
      "style": "body",
      "tags": [],
      "notes": []
    }
  ]
}
```

Derived files:

- `scene-0001.txt`: automatic plain text export.
- `scene-0001.meta.json`: title, synopsis, tags, status, counts, hashes.
- `project.sqlite`: fast lookup, binder tree, search index, shortcut settings, command assignments, window presets, session logs.

Recovery priority:

1. `*.wwdoc.json` is the manuscript source of truth.
2. `*.txt` is a readable derived backup.
3. `*.meta.json` and `project.manifest.json` help reconstruct project structure.
4. SQLite is the fast working index and settings store.

The app must be recoverable from files outside the app.

## Large Text Strategy

The app must never load the entire project into the editor.

Rules:

- Load only the active document for editing.
- Keep binder, outline, search, and counts in indexed metadata.
- Use background indexing for search and statistics.
- Keep autosave incremental at the active document level.
- Benchmark editor behavior with Korean IME and large pasted text before adding heavy rich-text features.
- Prefer plain text and paragraph records over heavyweight rich document formats for the core manuscript.

Editor candidate for MVP:

- Start with a WPF editor surface that edits one document as text.
- Measure WPF TextBox/RichTextBox limits early.
- If native controls lag with large documents or Korean IME, evaluate a proven WPF editor component such as AvalonEdit before writing a custom virtualized editor.

The MVP does not promise a full word processor. It promises stable, fast manuscript editing with recoverable local files.

## Preview

Preview is required from the first usable version.

MVP preview:

- Shows current document as formatted reading text.
- Updates from the structured paragraph model.
- Can switch between current document and included manuscript sequence later.

Preview must not require HTML as the source format.

## Autosave And Manual Save

Autosave is required.

Behavior:

- Autosave active document after a short debounce.
- Manual save is always `Ctrl+S`.
- Manual save writes canonical document, derived TXT, metadata, and SQLite index update.
- Save state is visible in the status strip.
- Save failure is visible and does not silently discard text.

The exact autosave interval is configurable later. MVP can start with a conservative fixed interval after typing pauses.

## Chrome Bridge

MVP excludes AI and automated web control.

Later Chrome bridge behavior:

- Open the user's normal Chrome.
- Do not embed login pages.
- Do not read Chrome cookies, passwords, or session tokens.
- Do not automate accounts unless the user explicitly approves a future safe design.

This feature is a launcher bridge, not a hidden browser wrapper.

## AI And API

MVP excludes Codex integration, Sakana/Fugu, GPT API, Gemini API, and any other cloud AI.

Reason:

- Private manuscripts should not be sent externally by default.
- Subscription chat products are not a clean app API.
- Cloud model wrappers introduce routing and privacy ambiguity.

Later AI integration must be explicit, opt-in, and visible:

- User supplies API key.
- User chooses which text is sent.
- App shows provider, endpoint, and purpose.
- Offline features continue working without AI.

## MVP Scope

MVP 0.1 builds the smallest real native workbench:

- WPF / .NET Windows app skeleton.
- Project folder creation under `C:\WriterWorkbench\Projects`.
- Binder with a small document tree.
- One active document editor.
- Live preview.
- Manual save with `Ctrl+S`.
- Autosave.
- Canonical `*.wwdoc.json`.
- Automatic `*.txt` export.
- SQLite metadata/index/settings store.
- Command registry.
- Shortcut manager with conflict detection.
- Assignable command bar slots.
- Focus session timer with fullscreen and early-exit challenge.
- Basic workspace preset save/apply for one monitor.

MVP 0.2 adds:

- Detached tab windows.
- Multi-monitor workspace presets.
- Startup preset auto-apply.
- Session logs.
- Better search.
- Paragraph tags and review flags.

MVP 0.3 adds:

- Plot/person/reference panels.
- Cover layout tools.
- Data cleanup exports.
- Rule-based manuscript checker.
- Visualization.

Out of scope for MVP:

- Tauri.
- Electron.
- Qt.
- Embedded login browser.
- Codex integration.
- Cloud AI/API calls.
- Full word-processor layout engine.
- Hard OS lockout.

## Security And Privacy

Default behavior:

- No internet required.
- No external text transmission.
- No browser session scraping.
- No hidden account automation.
- No system clock changes.
- No aggressive process control.

Local files:

- Project data lives in user-visible folders.
- API keys, if added later, must not be stored inside manuscript project folders.
- Backups and TXT exports must be visible and user-controllable.

## Open Questions

These are the next decisions before implementation:

- Which minimal .NET SDK installation path is acceptable, if any?
- Should the first editor use a native WPF control first, or should AvalonEdit be evaluated immediately?
- Should canonical paragraph files be one JSON document per scene, or JSONL records for very large scenes?
- What should the default focus timer duration be?
