param()
$src = Join-Path $env:USERPROFILE '.codex'
if (!(Test-Path $src)) { Write-Host "Codex data not found at $src"; exit 1 }
$out = Join-Path $PSScriptRoot 'codex-history-export.zip'
$tmp = Join-Path $env:TEMP ('codex-export-' + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tmp | Out-Null
Copy-Item -Recurse -Force (Join-Path $src 'sessions') $tmp
if (Test-Path (Join-Path $src 'archived_sessions')) { Copy-Item -Recurse -Force (Join-Path $src 'archived_sessions') $tmp }
Copy-Item -Force (Join-Path $src 'session_index.jsonl') $tmp
"This export contains Codex history in native format.`r`nCopy the contents into the target machine's %USERPROFILE%\\.codex directory.`r`nClose Codex before importing.`r`n" | Set-Content -Encoding ASCII (Join-Path $tmp 'README.txt')
if (Test-Path $out) { Remove-Item -Force $out }
Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $out
Remove-Item -Recurse -Force $tmp
Write-Host "Export complete: $out"
