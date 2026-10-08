# 2026-06-30 KST Midwork Recovery Audit

## Recovery target found

The broken midwork item was not missing source code. It was the damaged text rewrite recorded in `docs/pingpong.txt`:

- Broken commit: `afc9f79` (`Remove split editing from manuscript editor`)
- Repair commit: `ae1b5f8` (`Repair UTF-8 text and full editor invariant`)

The repair restored the core manuscript invariant:

- A manuscript editor must contain the full scene text.
- Large scenes must not be split into a hidden remainder panel.
- Metrics may be optimized, but the editor content itself must not hide part of the manuscript.
- Source files must not be rewritten through broad PowerShell text recoding that can damage BOM-less UTF-8 Korean text.

## Current recovery state

The current local source/test/doc files were compared against the public repo state after `67df616` (`Cover taskbar in focus and fullscreen`). Recovery-critical files matched the public repo:

- `src/WriterWorkbench/WebWorkbench/app.js`
- `src/WriterWorkbench/WebWorkbench/index.html`
- `src/WriterWorkbench/MainWindow.xaml.cs`
- `tests/WriterWorkbench.Tests/DocumentEditorTextServiceTests.cs`
- `tests/WriterWorkbench.Tests/WebWorkbenchAssetTests.cs`
- `tests/WriterWorkbench.Tests/EncodingDamageGuardTests.cs`
- `tests/WriterWorkbench.Tests/EncodingDamageGuard.cs`
- `docs/pingpong.txt`

The local Git history is older than the public repo history, but the working source content is not lost. The canonical recoverable state is the public repo plus the matching local working files.

## Verification run

Commands run locally:

```powershell
dotnet build WriterWorkbench.sln --no-restore
node --check src\WriterWorkbench\WebWorkbench\app.js
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter "DocumentEditorTextServiceTests|WebWorkbenchDoesNotSplitManuscriptIntoRemainderPanel|EncodingDamageGuardTests"
```

Results:

- Build passed: 0 warnings, 0 errors.
- HTML app script syntax check passed.
- Full test suite passed: 256 tests.
- Recovery guard tests passed: 8 tests.

## Guardrails already present

The recovered state is protected by tests that assert:

- Large documents open as full editable text.
- Large document edits preserve the whole paragraph set.
- HTML workbench does not contain the old remainder panel or hidden remainder text fields.
- HTML live metrics no longer use hidden baseline remainder fields.
- Known UTF-8 mojibake markers are absent from `src`, `tests`, and `docs`.

## Recovery decision

No rewrite from zero is required for this recovery item.

The broken work was found, the repaired invariant is present, and the current source state matches the public repo's latest recoverable implementation.

## Remaining operational risk

The local workspace Git repository itself is not a clean mirror of the public repo history. It has an older local commit base with many files represented as untracked or modified. Do not treat local `git status` alone as proof that implementation files are missing. Use the public repo and hash/file comparison as the recovery baseline unless the local repository metadata is intentionally rebuilt later.
