# Codex History Browser

Windows desktop tool for browsing, renaming, moving, exporting, and importing local Codex session history.

## Features

- Browse local Codex logs under `.codex`
- Rename sessions
- Change the workspace folder path of a session
- Move sessions between different devices with export/import packages
- Chinese / English UI support

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
