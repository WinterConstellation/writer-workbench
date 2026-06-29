# Writer Workbench folder sprawl audit - 2026-06-30 KST

## Verdict

The source project is not duplicated ten times, but the work process created folder sprawl:

- Multiple obsolete publish folders under `dist/`.
- Test residue under `%TEMP%\WriterWorkbench*`.
- A separate public GitHub mirror under `%TEMP%\writer-workbench-public-20260626035210`.

This should stop. Going forward the working set is:

| Role | Path | Keep |
| --- | --- | --- |
| Canonical local workspace | `C:\Users\Masters\Documents\글` | Yes |
| Public repo mirror | `C:\Users\Masters\AppData\Local\Temp\writer-workbench-public-20260626035210` | Yes, until replaced by a cleaner clone path |
| Current executable artifact | `C:\Users\Masters\Documents\글\dist\WriterWorkbench-html-workbench` | Yes |
| Old publish artifacts | Other `dist\WriterWorkbench*` folders | Cleanup candidates |
| Test scratch folders | `%TEMP%\WriterWorkbench*Tests` | Cleanup candidates |

## Measured cleanup candidates

Dry-run on 2026-06-30 found:

- `%TEMP%\WriterWorkbenchLargeTests` - about 977.94 MB.
- `%TEMP%\WriterWorkbenchTests` - about 72.11 MB.
- `%TEMP%\WriterWorkbenchPresetTests` - about 0.20 MB.
- `%TEMP%\WriterWorkbenchSessionTests` - about 0.07 MB.
- `%TEMP%\WriterWorkbenchShortcutTests` - about 0.25 MB.
- `dist\WriterWorkbench` - about 26.60 MB.
- `dist\WriterWorkbench-html-workbench-next` - about 39.10 MB.
- `dist\WriterWorkbench-menu-remote` - about 74.63 MB.
- `dist\WriterWorkbench-relationship-map` - about 2.50 MB.
- `dist\WriterWorkbench-win-x64` - about 2.24 MB.
- `dist\WriterWorkbench-win-x64-p0input` - about 2.24 MB.

## Changes made

- Added `tools\clean-writer-workbench-artifacts.ps1`.
- The script defaults to dry-run and only deletes when called with `-Delete`.
- The script refuses to touch paths outside `dist` and `%TEMP%`.
- The script keeps `dist\WriterWorkbench-html-workbench`.
- Updated `LargeDocumentIntegrationTests` so its new temp project is deleted after the test.
- Added retry logic for SQLite file-lock cleanup after large-document test runs.

## Not done automatically

No existing folders were deleted in this pass. Deletion should be an explicit cleanup action because the candidates include old executable artifacts.

## Policy going forward

- Always publish to `dist\WriterWorkbench-html-workbench`.
- Do not create feature-named publish folders.
- Keep public sync in one known mirror path until a cleaner clone location is chosen.
- Large tests must clean their own temp roots.
- When old artifacts are no longer needed, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\clean-writer-workbench-artifacts.ps1 -Delete
```

