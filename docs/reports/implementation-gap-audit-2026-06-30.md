# Writer Workbench implementation gap audit - 2026-06-30 KST

This audit exists because prior handoff notes overstated several features as implemented when the real user-facing surface was only partial. The standard here is not "the file contains code"; it is "the user can operate it without the feature disappearing, resetting, or silently doing the wrong thing."

## Current verdict

The project is not empty, but it is not feature-complete. A large part of the workbench is MVP or partial implementation. The immediate process change is:

- Do not mark a feature complete from static anchors alone.
- Prefer one user-visible workflow at a time, with state persistence and restart behavior included.
- Keep an explicit gap table in this report and `docs/pingpong.txt`.
- Run build/tests after code changes, then sync the public repo when changes are made.

## Gap table

| Area | Actual state | Gap | Priority |
| --- | --- | --- | --- |
| Native topmost remote control | Exists, can render commands, move, resize, icon/title mode, toggle | Before this patch, position/size/display/visibility were not persisted in app session; user changes could reset | P0 fixed in this pass; manual visual check still needed |
| Remote flicker | Render signature and show-path guards exist | Visual confirmation still needed in the running app during typing/save refresh | P0 verify |
| Binder context actions | Code paths exist for context menu, create, rename, duplicate, delete, reorder, batch delete | Needs real UI pass: right-click, multi-select click behavior, drag reorder, trash restore should be manually verified together | P0 verify |
| Manuscript body display | HTML editor exists and large-scene support was attempted | User still reports hidden/non-visible text and unstable counts; this is not complete | P0 unresolved |
| Character counts | Metadata and local metric paths exist | User reports counts jumping between visible segment and total manuscript; must be treated as active bug until reproduced/fixed | P0 unresolved |
| Fullscreen/focus mode | F11 routing and monitor-bounds cover have tests | Needs manual multi-monitor/taskbar verification | P1 verify |
| Scene memo | Per-scene memo editor exists in HTML payload path | Memo overview/search/export is minimal; not a full memo management surface | P1 partial |
| Relationship map | Basic typed entities, relationships, node drag, layout, zoom, settings-book sync exist | Not yet a full whiteboard for world/event/person/synopsis/scene links; visual affordances and edit/delete workflow need polish | P1 partial |
| Settings/reference surfaces | Basic panels and add/update bridge exist | Still shallow; not a complete setting-book workflow | P1 partial |
| Text replacement | MVP add/delete/apply path exists | Needs broader workflow: presets, apply scope, confirmation/diff preview | P2 partial |
| Repeated-word analysis | MVP analysis path exists | Needs selected-document batch UX and study/export polish | P2 partial |
| Shortcut editing | WPF and HTML bridge paths exist | Needs end-to-end proof that custom shortcuts route correctly from HTML focus states | P1 partial |
| Custom spell checker/corpus | Not implemented | Needs separate design and storage model | P2 not implemented |
| Codex CLI bridge | Code exists | Security/UX boundary and failure behavior need audit before treating as usable | P2 partial/risky |
| Detached windows | Surface claim registry and detached workbench exist | Not yet true independent multi-window editing with robust conflict/state sync | P2 partial |

## Patch included with this audit

Remote control session persistence was added because it is a concrete example of the overstatement problem:

- Added `RemoteControlSessionState` to app session data.
- Existing session files without remote data now load with safe defaults.
- Remote visibility, docked/detached state, left/top, width/height, and display mode are captured.
- Remote icon-only/title changes raise a window event so `MainWindow` can persist them.
- `ShowRemoteControlLayer(recenter: false)` no longer overwrites saved detached coordinates just because the layer was hidden.

## Next repair order

1. P0 manuscript body and character-count correctness.
2. P0 binder workflow manual verification plus missing fixes.
3. P1 relationship map edit/delete/whiteboard workflow polish.
4. P1 settings/reference surfaces as actual work surfaces, not placeholders.
5. P1 shortcut editing end-to-end verification from HTML focus states.
6. P2 text replacement presets and correction-corpus/spell-check design.

## Verification standard going forward

For UI-heavy changes, use this minimum:

- Static asset syntax check where relevant.
- `dotnet build WriterWorkbench.sln --no-restore`.
- Targeted tests for the changed workflow.
- Full `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore` before publish/sync.
- Manual visual note if the behavior depends on actual monitor/window state.

