# Writer Workbench

Windows-only WPF writing workbench for long-form manuscript work.

## Current Focus

- Local project folders under `C:\WriterWorkbench\Projects`
- Binder-style document list
- Editable manuscript scenes
- Manual save and idle autosave
- Derived `.txt` export beside structured document JSON
- SQLite search index
- Preview screen opened only by user action
- Detached document window
- Workspace window presets
- Startup session restore
- Graphic color presets
- Focus writing timer
- Large-document stress path with bounded editable segments

## Build

Requirements:

- Windows
- .NET 8 SDK

```powershell
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore
dotnet build WriterWorkbench.sln --no-restore
dotnet publish src\WriterWorkbench\WriterWorkbench.csproj -c Release -r win-x64 --self-contained false -o dist\WriterWorkbench-win-x64
```

Run:

```powershell
dist\WriterWorkbench-win-x64\WriterWorkbench.exe
```

## Notes

This is an early native prototype. AI/API integration and embedded web login automation are intentionally excluded from the MVP.
