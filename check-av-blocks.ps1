# Tests whether antivirus is blocking exe creation in the folders the overlay build/install uses.
# Safe: copies an UNSIGNED exe (the overlay build; signed exes like notepad are never blocked)
# into each folder, reports the result, deletes the copy.
$folders = @(
  "$env:LOCALAPPDATA\HudManagerOverlay",
  "$env:LOCALAPPDATA\Temp\hudsetup",
  "C:\HudOverlayTest",
  "$PSScriptRoot\HudManagerOverlay\bin\Release\net8.0-windows\win-x64",
  "$PSScriptRoot\HudManagerOverlay\bin\Debug\net8.0-windows\win-x64"
)
$cands = @("C:\HudOverlayTest\current\HudManagerOverlay.exe") + @(Get-ChildItem "$PSScriptRoot\HudManagerOverlay\bin" -Recurse -Filter HudManagerOverlay.exe -ErrorAction SilentlyContinue | ForEach-Object FullName)
$src = $cands | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $src) { throw "No unsigned overlay exe found to test with. Build first (dotnet build -c Debug)." }
"Test file: $src"
$blocked = @()
foreach ($f in $folders) {
  New-Item -ItemType Directory -Force $f | Out-Null
  $t = Join-Path $f "avtest.exe"
  try {
    Copy-Item $src $t -Force -ErrorAction Stop
    Start-Sleep -Milliseconds 2500          # give real-time scanners time to react
    if (Test-Path $t) { Remove-Item $t -Force; "OK       $f" }
    else { "DELETED  $f"; $blocked += $f }
  } catch { "BLOCKED  $f  ($($_.Exception.Message))"; $blocked += $f }
}
""
if ($blocked.Count -eq 0) { "All folders accept new unsigned exes. No exclusions needed." }
else {
  "Add these as FOLDER exclusions in Bitdefender (Protection > Antivirus > Settings > Manage exceptions),"
  "with every toggle on (incl. Advanced Threat Defense):"
  $blocked | ForEach-Object { "  $_" }
}
