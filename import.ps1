param()
$zip = Join-Path $PSScriptRoot 'codex-history-export.zip'
if (!(Test-Path $zip)) { Write-Host "Export zip not found: $zip"; exit 1 }
$dest = Join-Path $env:USERPROFILE '.codex'
if (!(Test-Path $dest)) { Write-Host "Codex data folder not found at $dest"; exit 1 }
$tmp = Join-Path $env:TEMP ('codex-import-' + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tmp | Out-Null
Expand-Archive -Path $zip -DestinationPath $tmp -Force
Copy-Item -Recurse -Force (Join-Path $tmp 'sessions') $dest
if (Test-Path (Join-Path $tmp 'archived_sessions')) { Copy-Item -Recurse -Force (Join-Path $tmp 'archived_sessions') $dest }
Copy-Item -Force (Join-Path $tmp 'session_index.jsonl') $dest
Remove-Item -Recurse -Force $tmp
Write-Host "Import complete. Restart Codex."
