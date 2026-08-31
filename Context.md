# Git Changes — project context

Read this file before inspecting source files. Keep it current when behavior or structure changes.

## Purpose

Lightweight desktop viewer for local Git changes. Built with C#, .NET 10, and Avalonia UI 12.1.1.

## Structure

- `MainWindow.axaml`: the complete single-window UI and visual styles.
- `MainWindow.axaml.cs`: window state, user actions, and presentation models.
- `GitRepository.cs`: Git process calls and change classification.
- `RecentProjectStore.cs`: persistence for up to eight recent repositories.
- `App.*`, `Program.cs`: minimal Avalonia startup.

## UI behavior

- `Project` opens an in-window recent-project list; the centered full-width `+` row invokes the folder picker.
- `Project` and `+` have transparent idle states and highlight only on actual pointer hover.
- The changes and diff panels are resized with the invisible splitter between them.
- Status colors: green added/new, amber modified, red deleted, blue renamed, purple staged.
- Added and removed diff lines use muted green and red backgrounds.
- Each tracked diff hunk has an `Undo` action with confirmation; untracked files are never deleted.
- Scrollbar hit areas are 16 px; visible thumbs are 4 px normally and 8 px on immediate hover.
- Clicking outside the in-window project list closes it.

## Development constraints

- Keep code small and direct; add abstractions only when they reduce complexity.
- Preserve the separation between Git operations and UI concerns.
- Verify changes with `dotnet build -c Release --nologo` and formatting checks.
- Do not run the application with `dotnet run`.
