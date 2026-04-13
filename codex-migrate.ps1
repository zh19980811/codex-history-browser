Write-Host "Codex History Migration"
Write-Host "1. Export (source machine)"
Write-Host "2. Import (target machine)"
$choice = Read-Host "Select 1 or 2"
$base = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($choice -eq '1') {
  & "$base\export.cmd"
  exit $LASTEXITCODE
}
if ($choice -eq '2') {
  & "$base\import.cmd"
  exit $LASTEXITCODE
}
Write-Host "Invalid selection."
exit 1
