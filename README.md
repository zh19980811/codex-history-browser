# Codex History Browser

Windows desktop tool for browsing, renaming, moving, exporting, and importing local Codex session history.

## Features

- Browse local Codex sessions under `.codex`
- Group sessions by workspace
- Preview chat content inside the app
- Rename sessions and sync titles back to Codex metadata
- Move sessions to another workspace path
- Export selected sessions into a portable package
- Import exported sessions on another computer
- Chinese / English UI switch
- Crash log written to `%TEMP%\codex-history-browser\crash.log`

## Main Files

- `session_browser.cs`: main WinForms UI
- `session_manager.cs`: related session management helper app
- `export.ps1` / `import.ps1`: legacy migration scripts
- `export.cmd` / `import.cmd`: one-click wrappers

## Build

Use the .NET Framework C# compiler on Windows:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:codex-history-browser.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:Microsoft.VisualBasic.dll session_browser.cs
```

## Run

```powershell
.\codex-history-browser.exe
```

## Release Asset

The Windows portable executable is published through GitHub Releases instead of being tracked in git.
