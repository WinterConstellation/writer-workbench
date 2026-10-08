# HTML Workbench Regression Loop - 2026-06-27 KST

## Loop 1 Current Structure Report

Scope inspected:
- `src/WriterWorkbench/WebWorkbench/index.html`
- `src/WriterWorkbench/WebWorkbench/app.js`
- `src/WriterWorkbench/WebWorkbench/styles.css`
- `src/WriterWorkbench/MainWindow.xaml.cs`
- related smoke and asset tests

Observed structure:
- Main editing surface is now an embedded WebView2 HTML workbench.
- WPF still owns project storage, document loading/saving, commands, shortcuts, snapshots, exports, and native detached windows.
- HTML receives a payload from WPF and renders binder, editor, inspector, preview, shortcut list, remote editor, and floating remote.
- HTML sends only three message types to WPF at this point:
  - `command`
  - `activeScene.update`
  - `remoteSettings.update`

Observed regressions:
- Binder rows render metadata but do not select a document. The only binder row action is a double-click that sends `view.main.open`, which does not identify the scene.
- Binder has no HTML action bar and no context menu for rename, duplicate, delete, move, snapshot, export, or detach.
- WPF bridge has no `document.select` or `document.command` handler, so HTML cannot target a binder command at a specific scene.
- HTML floating remote is a `position: fixed` element inside the WebView. It cannot move outside the application window or above other Windows windows.
- WPF native remote layer already supports `Topmost`, `DragMove`, resize grip, and off-window coordinates, but current MainWindow code explicitly hides it whenever HTML workbench is visible.
- HTML remote move handle is text (`이동`) instead of a cross-arrow style icon.
- A focused test run for these regressions timed out and left dotnet/vstest processes alive; those processes were terminated before continuing.

Risk assessment:
- The HTML move to main surface preserved some visual shell but severed command routing for binder workflows.
- Treating the HTML remote as the primary remote conflicts with the original Windows desktop requirement: draggable, topmost, resizable, and allowed outside the host window.
- The command system exists, but HTML did not preserve enough context (`documentId`) to use it safely.

## Loop 1 External Review

Review stance:
- This implementation is not an acceptable migration. It moved visible surfaces before preserving the command contract.

Blocking findings:
- P0: Binder is a passive list. A writing cockpit cannot lose scene selection and binder commands.
- P0: Remote behavior was implemented on the wrong surface. A WebView child cannot satisfy "outside the window" or true topmost behavior.
- P1: Existing native remote tests were inverted to accept hidden native remote behavior, which encoded the regression.
- P1: HTML bridge messages are too generic. `command` is enough for global commands, not scene-scoped binder actions.

Required loop 1 improvements:
- Add HTML binder action bar and context menu anchors.
- Add JS binder selection and `document.command` message generation.
- Add WPF bridge handlers for `document.select` and `document.command`.
- Re-enable native remote layer while HTML workbench is visible.
- Change HTML remote move button to a cross-arrow icon and make it a secondary in-window shortcut, not the only remote.
- Add/repair tests that fail without these paths.

## Loop 2 Current Structure Report

Changes after loop 1:
- HTML binder now has action bar and context menu markup.
- HTML script now contains `document.select`, `document.command`, `showBinderContextMenu`, `hideBinderContextMenu`, and `sendBinderCommand`.
- WPF bridge now has `document.select` and `document.command` handling.
- Binder command routing no longer has to load the active document for commands that only need binder selection.
- Native remote can be toggled while HTML workbench is visible.

Remaining structural problems:
- `ShowHtmlWorkbenchSurface` still calls `HideNativeRemoteControlLayer`, so entering the HTML workbench can still make the actual draggable/topmost remote disappear.
- HTML still renders an in-window `floating-remote`. That element cannot leave the WebView, so it should not be treated as the real remote.
- Native remote drag handle still uses only a title bar area; it does not show the requested cross-arrow move glyph.
- HTML settings/reference rails still mostly display lists rather than complete work surfaces. This is lower priority than binder/remote restoration, but it contributes to the feeling that basic functions disappeared.

## Loop 2 External Review

Review stance:
- Loop 1 restored command routing, but remote ownership is still confused.

Blocking findings:
- P0: There must be exactly one primary remote: the native topmost window. The HTML in-window remote should not compete with it.
- P0: HTML workbench entry must not hide the native remote layer.
- P1: The native remote needs a visible move glyph, because the draggable area is otherwise not discoverable.
- P1: Settings/reference rails need a follow-up pass, but should not block the binder/remote fix.

Required loop 2 improvements:
- Remove `HideNativeRemoteControlLayer` from HTML workbench entry.
- Hide the HTML in-window floating remote by default so the native remote is the primary control.
- Add a cross-arrow move glyph to the native remote drag handle.
- Add tests that lock these remote ownership decisions.

## Loop Implementation Results

Loop 1 changes applied:
- Added HTML binder action bar.
- Added HTML binder context menu.
- Added `document.select` and `document.command` messages in `app.js`.
- Added WPF bridge handling for `document.select` and `document.command`.
- Added `ApplyHtmlBinderCommandAsync` so scene-scoped binder commands select the binder item before running.
- Avoided unnecessary document loading for binder-only commands such as move, rename, duplicate, and delete.

Loop 2 changes applied:
- Removed HTML workbench entry behavior that hid the native remote layer.
- Kept native remote available/topmost while HTML workbench is visible.
- Added a cross-arrow move glyph (`✥`) to the native remote handle.
- Hid the in-WebView floating remote by default so the native remote is the primary remote.

Verification:
- `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore --filter "WebWorkbenchHtmlContainsFullWorkbenchShellAnchors|WebWorkbenchScriptWiresRemoteDragTabsAndEditorUpdate|MainWindowHtmlWorkbenchKeepsNativeRemoteControlLayerAvailable|MainWindowHtmlBinderCommandSelectsSceneBeforeRunningBinderCommand|LayerIsTopmostDraggableAndRendersRemotePlacements|MainWindowShowingHtmlWorkbenchDoesNotHideVisibleNativeRemoteControlLayer|WebWorkbenchCssKeepsInWindowRemoteSecondary"` -> 7 passed.
- `node -e "new Function(require('fs').readFileSync('src/WriterWorkbench/WebWorkbench/app.js','utf8')); console.log('app.js syntax ok')"` -> passed.
- `dotnet build WriterWorkbench.sln --no-restore` -> 0 warnings, 0 errors.
- `dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore` -> 191 passed.

Remaining risk after two loops:
- Settings/reference rails are still not complete work surfaces.
- Relationship map in HTML is still a shell, while the functional relationship map remains native.
- Shortcut editing is still read-only in HTML.
