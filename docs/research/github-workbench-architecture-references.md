# GitHub Workbench Architecture References

Date: 2026-06-26 KST

Purpose: identify a trustworthy structure for Writer Workbench as customization,
commands, shortcuts, menus, remotes, and future HTML/WebView surfaces grow.

## Reference Pattern

Primary reference: Visual Studio Code contribution-point architecture.

- Repository: https://github.com/microsoft/vscode
- Docs repository page:
  https://github.com/microsoft/vscode-docs/blob/main/api/references/contribution-points.md

The useful abstraction is not VS Code's TypeScript implementation itself. The
useful structure is that a feature contributes commands, menus, and keybindings
as data. UI surfaces then render those declarations instead of owning feature
behavior directly.

## Applied To Writer Workbench

Writer Workbench now treats default workbench affordances as command
contributions:

- `AppCommand` = executable capability identity.
- `CommandPlacementContribution` = where the command appears by default.
- `ShortcutBinding` = default keyboard access.
- `CommandRegistry` = command catalog plus default placements and shortcuts.
- `WorkbenchCustomizationProfile` = user-editable override/loadout layer.

This keeps WPF buttons, floating remote controls, context menus, and future
WebView2 HTML surfaces pointed at the same command IDs.

## Why This Matters

As the app grows, a new feature should not require separate manual edits in
five unrelated UI surfaces. The target flow is:

1. Add command ID and handler.
2. Add command contribution metadata.
3. Let toolbar, remote, shortcuts, and profile bootstrap read the contribution.
4. Let user profiles override placement or shortcut later.

## Not Copied

No VS Code source code, UI, file format, branding, or extension host runtime was
copied. Only the architectural idea of command/menu/keybinding contributions was
adapted.
