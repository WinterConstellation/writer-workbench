# Reference Usage - 2026-06-27 KST

## Scope

This note records which external/local references were actually used in the HTML workbench repair pass.

## References Read

1. `docs/reference-locks/lll-structure-to-workbench.md`
2. `docs/research/lll-to-writer-workbench-plan.md`
3. `C:\Users\Masters\Downloads\writing-studio_14.html`

## What Was Borrowed As Abstract Structure

- LLL/structure notes:
  - HTML should be the main workbench shell, not a decorative sub-panel.
  - WPF owns local files, save/export/snapshot, native windows, shortcuts, and WebView bridge.
  - HTML owns top menu, rails, workbench surfaces, inspector-like panels, and remote/workspace surfaces.
  - Relationship/map data should stay in separate story JSON files and avoid reading manuscript bodies.

- `writing-studio_14.html`:
  - A dedicated relationship tab should not be a placeholder.
  - Relationship UI needs a character list, relationship list, and a canvas-like map surface.
  - The relationship screen should allow adding items from the surface, not only from hidden native controls.

## What Was Not Copied

- No LLL code, assets, names, formats, user data, browser storage, or cache content was copied.
- No UI was pixel-copied from the attached HTML.
- The attached HTML was used only to lock the presence of a real relationship work area: lists, add controls, and canvas.

## Applied In This Patch

- HTML relationship surface now contains:
  - character add controls
  - relationship add controls
  - character list
  - relationship list
  - map canvas
  - draggable nodes
- WPF bridge now accepts:
  - `story.entity.add`
  - `story.relationship.add`
  - `story.layout.update`
- `WebWorkbenchPayload` now carries story structure state for HTML rendering.

## Remaining Reference Debt

- Settings book and reference/resource panels still need full work surfaces.
- The relationship map is MVP only: no edit/delete UI yet, no auto layout, no timeline integration.
- Shortcut editing is still not fully migrated into the HTML shell.
